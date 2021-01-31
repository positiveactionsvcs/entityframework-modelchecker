# Entity Framework Model Checker

This is a development tool that allows an Entity Framework model to be verified that it matches a real SQL Server database.  

If there are any mismatches, these will be reported, and the developer can make changes to their SQL Server database or EF model to make sure they are in sync.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.  See deployment for notes on how to deploy the project on a live system.

### Prerequisites

* Visual Studio 2017
* .NET Framework 4.5 (already included in Windows 10)

### Installing

Clone the repository to a Windows machine with the above prerequisities installed.  To debug this project, you will need to add this project as a reference to another project where there is an Entity Framework model.

## Deployment

Build the project in RELEASE mode.  The compiled version of the DLL can be used in a Test project of another .NET solution, if that project is using Entity Framework.