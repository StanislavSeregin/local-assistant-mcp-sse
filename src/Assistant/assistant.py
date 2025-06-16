import uuid
import json
from typing import Annotated, AsyncGenerator, TypedDict, Sequence
from langchain_core.messages import BaseMessage, HumanMessage, ToolMessage, SystemMessage
from langchain_openai import ChatOpenAI
from langgraph.graph import END, StateGraph
from langgraph.graph.message import add_messages
from langgraph.checkpoint.memory import InMemorySaver
from langchain_mcp_adapters.client import MultiServerMCPClient
from langchain_core.runnables import RunnableConfig


class AgentState(TypedDict):
    messages: Annotated[Sequence[BaseMessage], add_messages]


class Assistant:
    def __init__(self, open_ai_url: str, model_name: str, api_key: str, temperature: float, system_prompt: str, mcp_url: str):
        self.open_ai_url = open_ai_url
        self.model_name = model_name
        self.api_key = api_key
        self.temperature = temperature
        self.system_prompt = system_prompt
        self.mcp_url = mcp_url
        self.history = []
        self.tools_by_name = None
        self.model = None
        self.graph = None

    async def initialize(self):
        mcp_client = MultiServerMCPClient({
            "default_server": {
                "transport": "sse",
                "url": self.mcp_url
            }
        })

        tools = await mcp_client.get_tools()
        self.tools_by_name = {tool.name: tool for tool in tools}
        self.model = ChatOpenAI(
            base_url=self.open_ai_url,
            model=self.model_name,
            api_key=self.api_key,
            temperature=self.temperature
        ).bind_tools(tools)

        self.graph = self._build_graph()
        return self

    def _build_graph(self):
        async def call_model(state: AgentState, config: RunnableConfig):
            system_prompt = SystemMessage(self.system_prompt)
            response = await self.model.ainvoke(
                [system_prompt] + state["messages"], config)
            return {"messages": [response]}

        async def tool_node(state: AgentState):
            outputs = []
            for tool_call in state["messages"][-1].tool_calls:
                tool_result = await self.tools_by_name[tool_call["name"]].ainvoke(
                    tool_call["args"])
                outputs.append(
                    ToolMessage(
                        content=json.dumps(tool_result),
                        name=tool_call["name"],
                        tool_call_id=tool_call["id"],
                    )
                )

            return {"messages": outputs}

        def should_continue(state: AgentState):
            messages = state["messages"]
            last_message = messages[-1]
            if not last_message.tool_calls:
                return "end"
            else:
                return "continue"

        workflow = StateGraph(AgentState)
        workflow.add_node("agent", call_model)
        workflow.add_node("tools", tool_node)
        workflow.set_entry_point("agent")
        workflow.add_conditional_edges("agent", should_continue, {
                                       "continue": "tools", "end": END})
        workflow.add_edge("tools", "agent")
        checkpointer = InMemorySaver()
        return workflow.compile(checkpointer=checkpointer)

    async def process_message(self, message: str) -> AsyncGenerator[str, None]:
        if not self.graph:
            raise RuntimeError("Agent not initialized")

        inputs = {"messages": self.history +
                  [HumanMessage(content=message, name="user")]}
        config = {"configurable": {"thread_id": uuid.uuid4()}}
        async for msg, _ in self.graph.astream(inputs, config=config, stream_mode="messages"):
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

        state_history_iter = self.graph.get_state_history(config)
        messages_from_graph = next(state_history_iter).values["messages"]
        self.history = messages_from_graph
