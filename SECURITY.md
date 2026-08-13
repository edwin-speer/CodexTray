# Security

Codex Tray deliberately delegates authentication to the local Codex app-server. It must not read, copy, log, or transmit Codex credentials.

The application opens a small set of fixed HTTPS links only after a user clicks the corresponding menu, hover-card, or notification control.

The application has no self-update path. Review and build new versions deliberately.

Security-sensitive changes include executable discovery, child-process launch arguments, app-server protocol handling, startup registration, and any new persistence or network behavior.
