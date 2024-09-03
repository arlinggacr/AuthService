#!/bin/bash

PORT=4433

# Check if the port is in use
if lsof -i :$PORT -t >/dev/null; then
    echo "Port $PORT is in use. Terminating the process..."
    lsof -i :$PORT -t | xargs kill -9
else
    echo "Port $PORT is free."
fi

# Start your project
dotnet run # or the specific command you use to start your project
