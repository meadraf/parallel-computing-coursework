# Inverted Index Server

A high-performance, concurrent inverted index server implementation in C# that provides real-time text search capabilities across multiple documents.

## Overview

This project implements an HTTP server that maintains an inverted index of words from text files. It supports:
- Real-time document indexing
- Concurrent search operations
- File system monitoring for automatic index updates
- Thread pool for handling multiple requests
- Reader-writer lock mechanism for thread-safe operations

## Features

- **Concurrent Processing**: Custom thread pool implementation for handling multiple search requests simultaneously
- **Real-time Updates**: FileSystemWatcher integration for automatic index updates when files are added or removed
- **Thread-safe Operations**: Implementation of reader-writer locks to ensure data consistency
- **HTML Content Support**: Built-in HTML tag stripping for processing HTML documents
- **RESTful API**: Simple HTTP GET endpoint for searching words in the index

## Build Instructions

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- Git (for cloning the repository)

### Building from Source

1. Clone the repository:
```bash
git clone https://github.com/meadraf/parallel-computing-coursework
cd inverted-index-server
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the project:
```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release
```

5. Run the application:
```bash
dotnet run --project InvertedIndexServer
```

### Building with Visual Studio

1. Open the solution file (.sln) in Visual Studio
2. Select the build configuration (Debug/Release)
3. Press F5 to build and run, or Ctrl+B to build without running

## Setup

1. Ensure you have .NET 8.0
2. Clone the repository
3. Make sure directory `IndexData` exists in your project root
4. Place your text files (*.txt) in the IndexData directory

## Usage

### Starting the Server

```csharp
const string baseUrl = "http://localhost:5003/";
var server = new InvertedIndexServer.InvertedIndexServer(4, baseUrl);
server.Start();
```

### Searching for Words

Send a GET request to the server with a word parameter:
```
GET http://localhost:5003/?word=example
```

The server will return:
- 200 OK with a JSON array of filenames containing the word
- 404 Not Found if the word isn't in any indexed files
- 400 Bad Request if the word parameter is missing
- 500 Internal Server Error if something goes wrong

### Stopping the Server

Type 'stop' in the console or call:
```csharp
server.Stop();
```

## Architecture

### Components

1. **InvertedIndexServer**: Main server class handling HTTP requests and coordinating components
2. **ThreadPool**: Custom implementation for concurrent request processing
3. **ConcurrentInvertedIndex**: Thread-safe inverted index data structure
4. **IndexDataWatcher**: File system monitor for real-time index updates
5. **InvertedIndexBuilder**: Initial index builder from text files

### Thread Safety

The system employs several concurrency mechanisms:
- ReaderWriterLockSlim for index access
- Thread-safe concurrent queue for request handling
- Synchronized dictionary operations for index modifications

## Error Handling

The server implements comprehensive error handling:
- Invalid requests return appropriate HTTP status codes
- File processing errors are logged but don't crash the server
- Thread pool gracefully handles task failures

## Performance Considerations

- Uses a thread pool to limit maximum concurrent operations
- Implements efficient reader-writer locks for better throughput
- Maintains thread safety without excessive locking
- Uses concurrent data structures for better performance
