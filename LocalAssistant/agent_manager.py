from typing import Annotated, AsyncGenerator, TypedDict, List

from langchain_core.messages import BaseMessage, HumanMessage, AIMessage
from langchain_core.prompts import ChatPromptTemplate
from langchain_openai import ChatOpenAI
from langgraph.graph import END, START, StateGraph
from langgraph.graph.message import AnyMessage, add_messages
from langgraph.prebuilt import ToolNode
from langchain_mcp_adapters.client import MultiServerMCPClient

class GraphState(TypedDict):
    messages: Annotated[list[AnyMessage], add_messages]

class AgentManager:
    def __init__(self, open_ai_url: str, model_name: str, api_key: str, temperature: float, system_prompt: str, mcp_url: str):
        self.open_ai_url = open_ai_url
        self.model_name = model_name
        self.api_key = api_key
        self.temperature = temperature
        self.system_prompt = system_prompt
        self.mcp_url = mcp_url
        self.history = []
        self.llm = None
        self.agent = None
        self.app = None

    async def initialize(self):
        self.llm = ChatOpenAI(
            base_url=self.open_ai_url,
            model=self.model_name,
            api_key=self.api_key,
            temperature=self.temperature
        )
        mcp_client = MultiServerMCPClient({
            "default_server": {
                "transport": "sse",
                "url": self.mcp_url
            }
        })
        tools = await mcp_client.get_tools()
        prompt = ChatPromptTemplate.from_messages([
            ("system", self.system_prompt),
            ("placeholder", "{messages}")
        ])
        self.agent = prompt | self.llm.bind_tools(tools)
        self.app = self._build_graph(self.agent, tools)
        return self

    def _build_graph(self, agent, tools):
        async def call_model(state, config):
            response = await agent.ainvoke(state, config)
            return {"messages": response}

        def should_continue(state):
            last_message = state["messages"][-1]
            if not last_message.tool_calls:
                return END
            else:
                return "tools"

        builder = StateGraph(GraphState)
        builder.add_edge(START, "agent")
        builder.add_node("agent", call_model)
        builder.add_node("tools", ToolNode(tools))
        builder.add_conditional_edges("agent", should_continue, ["tools", END])
        builder.add_edge("tools", "agent")
        builder.add_edge("agent", END)
        return builder.compile()

    async def process_message(self, message: str) -> AsyncGenerator[str, None]:
        if not self.app:
            raise RuntimeError("Agent not initialized")
        
        self.history.append(HumanMessage(content=message, name="user"))
        inputs = {"messages": self.history}
        full_response = ""
        async for msg, metadata in self.app.astream(inputs, stream_mode="messages"):
            if msg.content and not isinstance(msg, HumanMessage):
                full_response += msg.content
                yield msg.content

        self.history.append(AIMessage(content=full_response))
    