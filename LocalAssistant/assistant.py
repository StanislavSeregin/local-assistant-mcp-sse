import uuid
from typing import Annotated, AsyncGenerator, TypedDict
from langchain_core.messages import HumanMessage
from langchain_core.prompts import ChatPromptTemplate
from langchain_openai import ChatOpenAI
from langgraph.graph import END, START, StateGraph
from langgraph.graph.message import AnyMessage, add_messages
from langgraph.prebuilt import ToolNode
from langgraph.checkpoint.memory import InMemorySaver
from langchain_mcp_adapters.client import MultiServerMCPClient

class GraphState(TypedDict):
    messages: Annotated[list[AnyMessage], add_messages]

class Assistant:
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
        checkpointer = InMemorySaver()
        return builder.compile(checkpointer=checkpointer)
    
    async def process_message(self, message: str) -> AsyncGenerator[str, None]:
        if not self.app:
            raise RuntimeError("Agent not initialized")

        inputs = {"messages": self.history + [HumanMessage(content=message, name="user")]}
        config = {"configurable": {"thread_id": uuid.uuid4()}}
        async for msg, _ in self.app.astream(inputs, config=config, stream_mode="messages"):
            if msg.type == "AIMessageChunk":
                if msg.tool_calls:
                    for tool_call in msg.tool_calls:
                        yield (
                            f"**{tool_call["name"]}** tool requested with args:\n"
                            "```json\n"
                            f"{tool_call["args"]}\n"
                            "```\n"
                        )

                else:
                    yield msg.content

            elif msg.type == "tool":
                yield (
                    f"**{msg.name}** tool response:\n"
                    "```txt\n"
                    f"{msg.content}\n"
                    "```\n"
                )

        state_history_iter = self.app.get_state_history(config)
        messages_from_graph = next(state_history_iter).values["messages"]
        self.history = messages_from_graph
