import asyncio
from typing import Annotated, TypedDict

from langchain_core.messages import HumanMessage
from langchain_core.prompts import ChatPromptTemplate
from langchain_core.tools import tool
from langchain_openai import ChatOpenAI
from langgraph.graph import END, START, StateGraph
from langgraph.graph.message import AnyMessage, add_messages
from langgraph.prebuilt import ToolNode

OLLAMA_MODEL="qwen3:30b-a3b-q8_0"
OLLAMA_URL="http://host.docker.internal:11434"

class GraphState(TypedDict):
    messages: Annotated[list[AnyMessage], add_messages]

@tool
def get_weather() -> str:
    """Inform the user that the weather is 15°C and it would rain and throw a joke in there also, but keep it brief 20 words max."""
    return "Inform the user that the weather is 15°C and it would rain and throw a joke in there also, but keep it brief 20 words max."

prompt = ChatPromptTemplate.from_messages(
    [
        (
            "system",
            """
            You are a helpful AI assistant specializing in weather. Provide accurate and clear weather information,
            forecasts, and safety tips based on user input. Offer localized details when provided with a location and
            explain weather phenomena concisely. If information is unclear or unavailable, ask for clarification. Be 
            user-friendly and reliable. DO NOT respond with more than 20 words.
            """,
        ),
        ("placeholder", "{messages}"),
    ]
)

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
        base_url=OLLAMA_URL + "/v1",
        model=OLLAMA_MODEL,
        temperature=0.1,
        api_key="ollama"
    )

    tools = [get_weather]
    agent = prompt | llm.bind_tools(tools)
    async for msg in run_graph("What's the weather like in Tokyo?", agent, tools):
        print(msg, end="|", flush=True)

if __name__ == "__main__":
    async def main():
        await test_chatopenai_with_tools()

    asyncio.run(main())
