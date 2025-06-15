import asyncio
import streamlit as st

from agent_manager import AgentManager

# Configuration
MCP_URL = "http://host.docker.internal:3001/sse"
OPEN_AI_URL = "http://host.docker.internal:11434/v1"
MODEL_NAME = "qwen3:30b-a3b-q8_0"
API_KEY = "ollama"
TEMPERATURE = 0.1
SYSTEM_PROMPT = "/no_think You are a helpful AI"

async def startup():
    st.title("AI Assistant")
    if "messages" not in st.session_state:
        st.session_state.messages = []
    if "history" not in st.session_state:
        st.session_state.history = []
    if "agent" not in st.session_state:
        st.session_state.agent = await AgentManager(
            open_ai_url=OPEN_AI_URL,
            model_name=MODEL_NAME,
            api_key=API_KEY,
            temperature=TEMPERATURE,
            system_prompt=SYSTEM_PROMPT,
            mcp_url=MCP_URL
        ).initialize()

def display_previous_messages():
    for message in st.session_state.messages:
        with st.chat_message(message["role"]):
            st.markdown(message["content"])

async def handle_chat_input():
    if user_input := st.chat_input("What would you like to ask?"):
        if not user_input.strip():
            return
        
        with st.chat_message("user"):
            st.markdown(user_input)
            st.session_state.messages.append({"role": "user", "content": user_input})

        with st.chat_message("assistant"):
            response_placeholder = st.empty()
            full_response = ""
            async for chunk in st.session_state.agent.process_message(user_input, st.session_state.history):
                full_response += chunk
                response_placeholder.markdown(full_response)
            
            st.session_state.agent.put_response_to_history(st.session_state.history, full_response)
            st.session_state.messages.append({"role": "assistant", "content": full_response})

async def main():
    await startup()
    display_previous_messages()
    await handle_chat_input()

if __name__ == "__main__":
    asyncio.run(main())
