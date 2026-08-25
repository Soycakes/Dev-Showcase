Built a full stack planning harness where Gemini coordinator agent routes tasks through multi layered agent to agent conversation, displaying decisions using Server Sent Events.

Engineered ReAct tool use loop with a shared file cache across agents, logging live file read events to frontend with asyncio.Queue.

Lowered hallucinated rates by 90% with auditor layer verifying with codebase and injecting AST extracted routes directly into worker context.

Wrote unit and integration tests for scaling agent numbers, verifying graph routing and sandbox execution in CI without live API calls.
