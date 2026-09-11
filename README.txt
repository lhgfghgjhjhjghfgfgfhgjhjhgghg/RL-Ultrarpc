RL-UltraRPC v2

This package is made for GitHub Actions to build a Windows x64 EXE.

1. Create a Discord Application in the Discord Developer Portal and copy its Application ID.
2. Put that ID into config.json.
3. Upload the whole RL-UltraRPC folder to an empty GitHub repository.
4. Keep .github/workflows/build.yml exactly where it is.
5. Commit the files.
6. Open GitHub > Actions > Build RL-UltraRPC.
7. Download the RL-UltraRPC-win-x64 artifact after the build succeeds.
8. Extract it and run RL-UltraRPC.exe.

Rocket League setup before launching the game:
[TAGame.MatchStatsExporter_TA]
PacketSendRate=10
Port=49123
WebPort=49124

The EXE is intentionally lightweight: native Windows process, no console, no Python, no Electron, no BakkesMod/injection.
Discord desktop must be running.
