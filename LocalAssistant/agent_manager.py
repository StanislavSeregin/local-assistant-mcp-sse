from typing import Annotated, AsyncGenerator, TypedDict

from langchain_core.tools import BaseTool
from langchain_core.messages import HumanMessage
from langchain_core.prompts import ChatPromptTemplate
from langchain_openai import ChatOpenAI
from langgraph.graph import END, START, StateGraph
from langgraph.graph.message import AnyMessage, add_messages
from langgraph.prebuilt import ToolNode

class GraphState(TypedDict):
    messages: Annotated[list[AnyMessage], add_messages]

class AgentManager:
    def __init__(self, ollama_url: str, ollama_model: str, tools: list[BaseTool]):
        self.ollama_url = ollama_url
        self.ollama_model = ollama_model
        self.llm = ChatOpenAI(
            base_url=self.ollama_url,
            model=self.ollama_model,
            temperature=0.1,
            api_key="ollama"
        )
        self.agent = None
        self.app = None

        prompt = ChatPromptTemplate.from_messages([
            ("system", "/no_think You are a helpful AI"),
            ("placeholder", "{messages}")
        ])

        self.agent = prompt | self.llm.bind_tools(tools)
        self.app = self._build_graph(self.agent, tools)

    def _build_graph(self, agent, tools):
        async def call_model(state, config):
            response = await agent.ainvoke(state, config)
            return {"messages": response}

        def should_continue(state):
            last_message = state["messages"][-1]
            return "tools" if last_message.tool_calls else END

        builder = StateGraph(GraphState)
        builder.add_node("agent", call_model)
        builder.add_node("tools", ToolNode(tools))
        builder.add_edge(START, "agent")
        builder.add_conditional_edges("agent", should_continue, ["tools", END])
        builder.add_edge("tools", "agent")
        builder.add_edge("agent", END)
        return builder.compile()

    async def process_message(self, message: str) -> AsyncGenerator[str, None]:
        if not self.app:
            raise RuntimeError("Agent not initialized")

        inputs = {"messages": [HumanMessage(content=message, name="user")]}
        async for msg, metadata in self.app.astream(inputs, stream_mode="messages"):
            if msg.content and not isinstance(msg, HumanMessage):
                yield msg.content