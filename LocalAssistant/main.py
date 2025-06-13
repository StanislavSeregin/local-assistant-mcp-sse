import asyncio
from typing import Annotated, TypedDict

from langchain_core.messages import HumanMessage
from langchain_core.prompts import ChatPromptTemplate
from langchain_openai import ChatOpenAI
from langgraph.graph import END, START, StateGraph
from langgraph.graph.message import AnyMessage, add_messages
from langgraph.prebuilt import ToolNode

from mcp import ClientSession
from mcp.client.sse import sse_client
from langchain_mcp_adapters.tools import load_mcp_tools

OLLAMA_MODEL="qwen3:30b-a3b-q8_0"
OLLAMA_URL="http://host.docker.internal:11434/v1"
MCP_URL="http://host.docker.internal:3001/sse"

TEST_PROMPT="Display files listing from disk"

class GraphState(TypedDict):
    messages: Annotated[list[AnyMessage], add_messages]

prompt = ChatPromptTemplate.from_messages([
    ("system", "/no_think You are a helpful AI"),
    ("placeholder", "{messages}")
])

def build_graph(agent, tools):
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


async def run_graph(input_message, agent, tools):
    app = build_graph(agent, tools)
    inputs = {"messages": [HumanMessage(content=input_message, name="user")]}
    async for msg, metadata in app.astream(inputs, stream_mode="messages"):
        if msg.content and not isinstance(msg, HumanMessage):
            yield msg.content

async def test_chatopenai_with_tools():
    llm = ChatOpenAI(
        base_url=OLLAMA_URL,
        model=OLLAMA_MODEL,
        temperature=0.1,
        api_key="ollama"
    )

    async with sse_client(url=MCP_URL) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            mcp_tools = await load_mcp_tools(session)
            agent = prompt | llm.bind_tools(mcp_tools)
            async for msg in run_graph(TEST_PROMPT, agent, mcp_tools):
                print(msg, end="", flush=True)

if __name__ == "__main__":
    async def main():
        await test_chatopenai_with_tools()

    asyncio.run(main())
