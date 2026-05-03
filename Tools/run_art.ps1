$env:RECRAFT_API_KEY = "IfhuPy8mkyMTRaLDel4tCsIBa747ILbIXdrCcJcmBAV8aINdpeQQkNahKKjah0pC"
& "C:\Users\YZX06\AppData\Local\Python\bin\python.exe" "$PSScriptRoot\generate_last_kernel_art.py" @args

# Usage:
#   .\run_art.ps1                   # generate all (skip existing)
#   .\run_art.ps1 --make-style      # preview reference, approve, then regenerate all
#   .\run_art.ps1 --regen Warrior   # regenerate a single card
