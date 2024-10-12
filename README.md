# EXTW - EXT Wannabe File System

## !!! WORK IN PROGRESS !!!
This project is still WIP.<br>
Changes are to be done at random and would aim to only refactor the code, not add any new functionalities.<br>
At the end the project should mostly do the same things as it has been doing until now, with some QOL improvements and better code readibility.<br>

## Project Overview
EXTW (EXT Wannabe) is a project, that simulates an EXT FileSystem. It is mostly closer to the EXT2 version from 1993.<br>
At its core it is meant to create a simple FileSystem in a binary file.

EXTW provides basic commands such as:
- `mkdir <filepath>` : Create a directory at the specified filepath.
- `write <filepath> "<content>"` : Create a file at the specified filepath.
- `cd <filepath>` : Change to the specified directory.
- `rm <filepath>` : Delete a file or directory at the specified filepath.
- etc.

## Known Bugs / Impropper Implementations
- It partially supports relative filepaths. <br> (by partially I mean I've seen some exceptions in the creation of a file that it does infact not use the given relative path, so for now it is higly requested to use the absolute filepath, if the relative one does not work as intended.. it is obviously subject to be corrected.)

- File separators are '\\' (backslash). <br> (subject to change, since normally EXT uses '/' (forward slash).)

- At the start of using EXTW, the console will ask you for simple 3 step FileSystem configuration - block size, block quantity, inode quantity. <br> (since directory entries have static sizes (for EXTW being 32 bytes each) and the first two are reserved for the current and previous directory, this means that 'block size' % 64 should be equal to 0. so an optimal size of a block size should be 256-8kb. it is a subject to change how you would choose block sizes based on a set of options, not by typing it out.)

- EXTW will always statically create the filesystem binary file, on which the operations and storage takes place, in the solution's directory with filename: "fsys". <br> (this is intended, but the problem comes when you open the project again. the EXTW will ask you for configurations, even though the older fsys still exists. such event is handled, but in the wrong place, hence it is again subject to change. a QOL could be if EXTW detects an existing filesystem binary file, it will ask you if you would want to continue with it, delete it, or create a new one with a different name.)

## Dependencies
The project does not require any external frameworks or NuGet packages beyond the .NET Framework's built-in serialization tools (BinaryWriter/Reader).

## Prerequisites
1. Install the [.NET SDK](https://dotnet.microsoft.com/download/dotnet) (version 8.0 or higher).
2. (Optional) Install [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.microsoft.com/).

## Installation
To set up EXTW, follow these steps:

1. Clone the repository to your preferred directory:
   ```bash
   git clone https://github.com/Desperadko/EXTW.git

2. Navigate to the EXTW directory:
   ```bash
   cd EXTW

3. Build the solution:
   ```bash
   dotnet build EXTW.sln

4. Run the project:
   ```bash
   dotnet run EXTW/EXTW.cspoj

## Contributions
Feel free to submit issues or pull requests to enhance the functionality and usability of EXTW. 
