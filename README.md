# RawSocketMonitor

A Windows Forms application for monitoring raw network packets using C# and .NET.

## Project Summary
This project was created as a demonstration of my skills in low-level networking, multithreaded programming, and Windows desktop application development. It showcases the use of raw sockets to capture and analyze network traffic in real time, as well as the ability to build responsive and user-friendly interfaces with WinForms.

## Why this project?
RawSocketMonitor was developed to highlight:
- Knowledge of TCP/IP networking and socket programming
- Protocol parsing and real-time data display

## Features
- Captures TCP and UDP packets using a raw socket
- Displays source/destination IP addresses and ports
- Identifies common application protocols by port
- Multi-threaded packet capture for UI responsiveness
- Start/Stop capture from the menu
- Extensible protocol mapping

## Requirements
- Windows OS (raw sockets require administrative privileges)
- .NET 6.0 (or later) with Windows Forms support
- Visual Studio or `dotnet` CLI

## Usage
1. **Build the project:**
   - Open the solution in Visual Studio and build, or run `dotnet build` in the project directory.
2. **Run as Administrator:**
   - Raw sockets require admin rights. Right-click the executable and select "Run as administrator," or launch your terminal as admin before running `dotnet run`.
3. **Start/Stop Capture:**
   - Use the File menu to start or stop packet capture.
   - Captured packets will appear in the list view.

## Code Highlights
- Uses `System.Net.Sockets.Socket` with `SocketType.Raw` and `ProtocolType.IP`.
- Processes IP headers to extract protocol, ports, and addresses.
- Uses a background thread for packet capture to keep the UI responsive.
- Protocol/port mapping is handled via a dictionary for easy extension.

## Limitations
- Only works on Windows (due to raw socket and WinForms requirements).
- Requires administrative privileges.
- Captures only IPv4 traffic on the specified local interface.

## Author
Thai N.

