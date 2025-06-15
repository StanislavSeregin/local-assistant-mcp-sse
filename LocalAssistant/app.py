import asyncio
import streamlit as st

from langchain_mcp_adapters.client import MultiServerMCPClient

from agent_manager import AgentManager

# Конфигурация
OPEN_AI_URL = "http://host.docker.internal:11434/v1"
MODEL_NAME = "qwen3:30b-a3b-q8_0"
API_KEY = "ollama"
TEMPERATURE = 0.1
SYSTEM_PROMPT = "/no_think You are a helpful AI"

MCP_URL = "http://host.docker.internal:3001/sse"

async def process_user_input(user_input: str):
    if not user_input.strip():
        return

    # Добавляем сообщение пользователя в историю чата
    st.session_state.messages.append({"role": "user", "content": user_input})
    
    # Создаем placeholder для ответа агента
    with st.chat_message("assistant"):
        response_placeholder = st.empty()
        full_response = ""
        
        # Получаем потоковый ответ от агента
        async for chunk in st.session_state.agent.process_message(user_input):
            full_response += chunk
            response_placeholder.markdown(full_response)
        
        # Добавляем полный ответ в историю чата
        st.session_state.messages.append({"role": "assistant", "content": full_response})

def display_chat_history():
    for message in st.session_state.messages:
        with st.chat_message(message["role"]):
            st.markdown(message["content"])

if __name__ == "__main__":
    async def main():
        st.title("AI Assistant")
        if "agent" not in st.session_state:
            st.session_state.agent = None
        if "messages" not in st.session_state:
            st.session_state.messages = []
        if st.session_state.agent is None:
            agent_manager = AgentManager(
                open_ai_url=OPEN_AI_URL,
                model_name=MODEL_NAME,
                api_key=API_KEY,
                temperature=TEMPERATURE,
                system_prompt=SYSTEM_PROMPT,
                mcp_url=MCP_URL
            )
            await agent_manager.initialize()
            st.session_state.agent = agent_manager

        display_chat_history()
        if user_input := st.chat_input("What would you like to ask?"):
            await process_user_input(user_input)

    asyncio.run(main())