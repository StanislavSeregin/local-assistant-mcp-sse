import os
import asyncio
import streamlit as st
from dotenv import load_dotenv
from assistant import Assistant

load_dotenv()


async def startup():
    st.title("Helpful assistant")
    if "assistant" not in st.session_state:
        st.session_state.assistant = await Assistant(
            open_ai_url=os.environ['OPEN_AI_URL'],
            model_name=os.environ['MODEL_NAME'],
            api_key=os.environ['API_KEY'],
            temperature=os.environ['TEMPERATURE'],
            system_prompt=os.environ['SYSTEM_PROMPT'],
            mcp_url=os.environ['MCP_URL']
        ).initialize()


def render_chat_history():
    for message in st.session_state.assistant.history:
        match message.type:
            case "human":
                with st.chat_message("user"):
                    st.markdown(message.content)

            case "ai":
                with st.chat_message("assistant"):
                    if message.tool_calls:
                        content = ""
                        for tool_call in message.tool_calls:
                            content += (
                                f"**{tool_call["name"]}** tool requested with args:\n"
                                "```json\n"
                                f"{tool_call["args"]}\n"
                                "```"
                            )

                        st.markdown(content)

                    else:
                        st.markdown(message.content)

            case "tool":
                with st.chat_message("tool", avatar="🔧"):
                    st.markdown((
                        f"**{message.name}** tool response:\n"
                        "```txt\n"
                        f"{message.content}\n"
                        "```"
                    ))


async def handle_chat_input():
    if user_input := st.chat_input("What would you like to ask?"):
        if not user_input.strip():
            return

        with st.chat_message("user"):
            st.markdown(user_input)

        with st.chat_message("assistant"):
            full_response = ""
            response_placeholder = st.empty()
            async for chunk in st.session_state.assistant.process_message(user_input):
                full_response += chunk
                response_placeholder.markdown(full_response)

        st.rerun()


async def main():
    await startup()
    render_chat_history()
    await handle_chat_input()

if __name__ == "__main__":
    asyncio.run(main())
