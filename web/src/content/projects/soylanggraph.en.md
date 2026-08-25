Designed 4 layer LangGraph state machine with Gemini Flash agents for context analysis, planning, critique, and coding layers, with MemorySaver checkpoints for human approval gates.

Implemented dynamic parallel coworker design using LangGraph's Send API to allow an agent to send and split three biased reviewer agents concurrently.

Built a pre-approval safety net using Python AST parsing to catch syntax errors, and protected file denylist to block writes to framework files.

Wrote 78 unit tests with mocked LLM nodes for catching graph routing regressions in CI without live API calls.
