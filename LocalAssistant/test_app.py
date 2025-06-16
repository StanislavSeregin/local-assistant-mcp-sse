import os
import asyncio
from dotenv import load_dotenv
from agent_manager import AgentManager

load_dotenv()

async def main():
    agent = await AgentManager(
        open_ai_url=os.environ['OPEN_AI_URL'],
        model_name=os.environ['MODEL_NAME'],
        api_key=os.environ['API_KEY'],
        temperature=os.environ['TEMPERATURE'],
        system_prompt=os.environ['SYSTEM_PROMPT'],
        mcp_url=os.environ['MCP_URL']
    ).initialize()

    inputs = ["Hello!", "Display files from work directory"]
    for input in inputs:
        print(input)
        async for chunk in agent.process_message(input):
            print(chunk, end="|", flush=True)
    
if __name__ == "__main__":
    asyncio.run(main())
