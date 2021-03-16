DEL /F /Q C:\Users\mark\source\repos\chgcnf\chgcnf_x64.exe
DEL /F /Q C:\Users\mark\source\repos\chgcnf\chgcnf_x86.exe

COPY /Y C:\Users\mark\source\repos\chgcnf\bin\x64\Release\chgcnf3.exe C:\Users\mark\source\repos\chgcnf\chgcnf_x64.exe
COPY /Y C:\Users\mark\source\repos\chgcnf\bin\x86\Release\chgcnf3.exe C:\Users\mark\source\repos\chgcnf\chgcnf_x86.exe
