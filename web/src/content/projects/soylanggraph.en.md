![|sm|inline](/Dev-Showcase/media/LS_1.png)
![|sm|inline](/Dev-Showcase/media/LS_2.png)
![|sm|inline](/Dev-Showcase/media/LS_3.png)
![|sm|inline](/Dev-Showcase/media/LS_4.png)
![|sm|inline](/Dev-Showcase/media/SLG_1.png)
![|sm|inline](/Dev-Showcase/media/SLG_2.png)
![|sm|inline](/Dev-Showcase/media/SLG_3.png)

SoyLangGraph takes a plain English coding task and runs it through four layers of Gemini Flash agents, which cover context analysis, planning, critique and coding. What comes out is a diff for me to review, and nothing is written to disk until I approve it. It was my first project in LangGraph, and I wanted to see how far a small, editable graph of cheap agents could get.

The run starts with a workspace scan that reads the repo layout and the contents of any files that look relevant. The first layer reads the task against that context and drafts a plan with a list of target files. In interactive mode the graph stops here. I can approve the plan, reject it, or type a correction, and the layer reruns with my note. This is LangGraph's interrupt and `Command(resume)` pattern, and every step is checkpointed with `MemorySaver`. That is what lets the graph sit at a human gate and pick up from the exact same state afterward. There are two of these stops, plan review and diff approval, and neither one reruns any earlier node when it resumes.

Once the plan is approved, the next layer expands it into a file by file spec and decides whether the task is big enough to fan out. If it is, three coworker agents run in parallel through LangGraph's Send API, each biased on purpose. The minimalist pushes back on anything unnecessary, the cynic hunts for edge cases and failure modes, and the optimizer flags memory and compute costs. Their findings come back through a custom reducer and merge into the plan before any code is written.

The last layer writes the full content of each file. Before I see anything, the output goes through an AST gate that parses it with Python's `ast` module. If it fails to parse, the error message is fed back to the coding layer as context and it tries again. Retries are capped by circuit breakers, and once the limit is hit the graph hands off to a human node instead of looping. Code that parses cleanly is diffed against the current file on disk with `difflib`, and that diff is what I approve. A protected file denylist sits in front of the write step, so framework files cannot be touched no matter what the model returns.

A few decisions came from things that broke:

- **Files the scan missed.** If the first layer picked a file the workspace scan had not pre-loaded, the coding layer would work without its contents and fail without saying so. Now file contents are read at entry, and the coding layer falls back to reading from disk at synthesis time.
- **Dataclasses over Pydantic.** `MemorySaver` hands checkpointed state back as plain dicts. Pydantic models caused serialization mismatches there, so all state types are Python dataclasses.
- **Text only output.** Automatic function calling is turned off in the Gemini SDK so the agents return text and nothing else.
- **One config file.** All system prompts and model constants live in a single file, so changing an agent's behavior never means hunting through nodes.

LangSmith tracing is wired in through environment variables, with no instrumentation code in the graph. I used the dashboard to inspect state transitions, node timing and routing decisions, which is how I caught routing regressions and confirmed the fanout actually ran in parallel.

78 unit and integration tests cover every routing path and state transition. Each LLM node has a stub that switches on when there is no API key, so the whole suite runs in CI without a single API call.

This project was the foundation for MAPDev, which takes the same planning layers and puts them behind a real time group chat with a FastAPI backend and a React frontend.
