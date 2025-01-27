# CrioCode

A lightweight Lua code editor with syntax highlighting, built using C# and WPF. This editor provides basic editing and execution capabilities for Lua code with a convenient user interface.

## Features

### Code Editing

- `New` - Create new file
- `Open` - Open existing file 
- `Save` - Save current file (Ctrl+S)
- `Run` - Execute Lua code (F5)

### Interface

- Modern dark theme
- Lua syntax highlighting
- Multiple file tabs support
- Character counter
- Custom window chrome
- Code execution output in separate window

### Editor

- Line numbers
- Auto indentation
- Code folding
- Current line highlighting
- UTF-8 support

## Requirements

- .NET 8.0
- Windows

## Technologies Used

- WPF (Windows Presentation Foundation)
- AvalonEdit - Text editor component
- NLua - Lua interpreter for .NET

## About

CrioCode was created for educational purposes as a demonstration of WPF capabilities and text editor implementation. This project is not intended for production use.

## Project Structure

- `MainWindow.xaml` - Main application window UI
- `MainWindow.xaml.cs` - Main window logic
- `TabItem.cs` - Tab item model
- `Lua.xshd` - Lua syntax highlighting definition
