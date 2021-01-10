#!/bin/bash

#quit if the script is not run in a terminal
if [[ $TERM == dumb ]]; then
    exit
fi

#This is a necessary step to use alias?
shopt -s expand_aliases

#Alias "compile" to the command below to make the code look cleaner.
alias compile="dotnet publish -p:PublishSingleFile=true -p:PublishTrimmed=true --configuration Release -o build --self-contained --nologo"

printf "$(tput setaf 6)Publishing for Linux (x86-64)$(tput sgr0)\n"
compile -r linux-x64 -p:PublishReadyToRun=true
printf "$(tput setaf 6)Publishing for Windows (x86-64)$(tput sgr0)\n"
compile -r win-x64

#I put this unalias-command here just in case. Is this even necessary?
unalias compile

#remove debug files
rm -f build/*.pdb

#add a file-extension to the Linux executable
mv -f build/FileFinder build/FileFinder.x86-64 2>/dev/null

printf "$(tput setaf 6)Script exit$(tput sgr0)\n"
exit
