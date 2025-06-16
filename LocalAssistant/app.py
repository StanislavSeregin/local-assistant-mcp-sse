import os
import asyncio
import streamlit as st
from dotenv import load_dotenv
from agent_manager import AgentManager

load_dotenv()

async def startup():
    st.title("Helpful assistant")
    if "agent" not in st.session_state:
        st.session_state.agent = await AgentManager(
            open_ai_url=os.environ['OPEN_AI_URL'],
            model_name=os.environ['MODEL_NAME'],
            api_key=os.environ['API_KEY'],
            temperature=os.environ['TEMPERATURE'],
            system_prompt=os.environ['SYSTEM_PROMPT'],
            mcp_url=os.environ['MCP_URL']
        ).initialize()

def display_previous_messages():
    for message in st.session_state.agent.history:
        match message.type:
            case "human":
                with st.chat_message("user"):
                    st.markdown(message.content)
            case "ai":
                with st.chat_message("assistant"):
                    st.markdown(message.content)

async def handle_chat_input():
    if user_input := st.chat_input("What would you like to ask?"):
        if not user_input.strip():
            return
        
        with st.chat_message("user"):
            st.markdown(user_input)

        with st.chat_message("assistant"):
            response_placeholder = st.empty()
            full_response = ""
            async for chunk in st.session_state.agent.process_message(user_input):
                full_response += chunk
                response_placeholder.markdown(full_response)

async def main():
    await startup()
    display_previous_messages()
    await handle_chat_input()

if __name__ == "__main__":
    asyncio.run(main())
