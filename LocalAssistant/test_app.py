import asyncio

from agent_manager import AgentManager

MCP_URL = "http://host.docker.internal:3001/sse"
OPEN_AI_URL = "http://host.docker.internal:11434/v1"
MODEL_NAME = "qwen3:30b-a3b-q8_0"
API_KEY = "ollama"
TEMPERATURE = 0.1
SYSTEM_PROMPT = "/no_think You are a helpful AI"

async def main():
    agent = await AgentManager(
        open_ai_url=OPEN_AI_URL,
        model_name=MODEL_NAME,
        api_key=API_KEY,
        temperature=TEMPERATURE,
        system_prompt=SYSTEM_PROMPT,
        mcp_url=MCP_URL
    ).initialize()

    inputs = ["Hello!", "Display files from work directory"]
    for input in inputs:
        print(input)
        async for chunk in agent.process_message(input):
            print(chunk, end="|", flush=True)
    
if __name__ == "__main__":
    asyncio.run(main())