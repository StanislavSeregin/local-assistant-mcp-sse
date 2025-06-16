import uuid
from typing import Annotated, AsyncGenerator, TypedDict, Sequence
from langchain_core.messages import BaseMessage, HumanMessage, SystemMessage
from langchain_ollama import ChatOllama
from langgraph.graph.message import add_messages
from langgraph.checkpoint.memory import InMemorySaver
from langchain_mcp_adapters.client import MultiServerMCPClient
from langgraph.prebuilt import create_react_agent


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
        self.graph = None

    async def initialize(self):
        model = ChatOllama(
            client_kwargs={"headers": {"Connection": "close"}},
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

        self.graph = create_react_agent(
            model=model,
            tools=tools,
            prompt=SystemMessage(self.system_prompt),
            version="v1",
            checkpointer=InMemorySaver()
        )

        return self

    async def process_message(self, message: str) -> AsyncGenerator[str, None]:
        if not self.graph:
            raise RuntimeError("Agent not initialized")

        inputs = {
            "messages": self.history + [HumanMessage(content=message, name="user")]
        }

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
        self.history = next(state_history_iter).values["messages"]
