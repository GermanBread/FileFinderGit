#!/bin/bash

#quit if the script is not run in a terminal
if [[ $TERM == dumb ]]; then
    exit;
fi

#Rundown of the available runtimes (I could've made an array, oh well)
printf "Please select runtime$(tput sgr0)\n";
printf "LINUX:\n";
printf "1 - Linux (x86-64)\n";
printf "2 - Linux (ARM-64)\n"
printf "3 - Linux (ARM-32)\n";
printf "WINDOWS:\n";
printf "4 - Windows (x86-64)\n";
printf "5 - Windows (x86-32)\n";
printf "6 - Windows (ARM-64)\n";
printf "7 - Windows (ARM 32)\n";
printf "\n";

#Repeat only if the user hasn't entered an integer...
while ! [[ $selection =~ ^[0-9]+$ ]]; 
do
    read selection;
    clear;
    #if the entered value was not an integer, show this
    if ! [[ $selection =~ ^[0-9]+$ ]]; then
        sleep 1;
        printf "$(tput setaf 9)Please try again$(tput sgr0)\n";
        printf "LINUX:\n";
        printf "1 - Linux (x86-64)\n";
        printf "2 - Linux (ARM-32)\n"
        printf "3 - Linux (ARM-64)\n";
        printf "WINDOWS:\n";
        printf "4 - Windows (x86-64)\n";
        printf "5 - Windows (x86-32)\n";
        printf "6 - Windows (ARM-64)\n";
        printf "7 - Windows (ARM 32)\n";
        printf "\n";
    fi
done

#This is a necessary step to use alias?
shopt -s expand_aliases

#Alias "compile" to the command below to make the code look cleaner.
alias compile="dotnet publish -p:PublishSingleFile=true -p:PublishTrimmed=true -o build";

#The user has sucessfully selected a runtime, now compile it.
case $selection in
    1)
    printf "$(tput setaf 6)Publishing for Linux (x86-64)$(tput sgr0)\n";
    compile -r linux-x64;
    ;;

    2)
    printf "$(tput setaf 6)Publishing for Linux (ARM-64)$(tput sgr0)\n";
    compile -r linux-arm64;
    ;;

    3)
    printf "$(tput setaf 6)Publishing for Linux (ARM-32)$(tput sgr0)\n";
    compile -r linux-arm;
    ;;
    
    4)
    printf "$(tput setaf 6)Publishing for Windows 10 (x86-64)$(tput sgr0)\n";
    compile -r win-x64;
    ;;

    5)
    printf "$(tput setaf 6)Publishing for Windows 10 (x86-32)$(tput sgr0)\n";
    compile -r win-x86;
    ;;

    6)
    printf "$(tput setaf 6)Publishing for Windows 10 (ARM-64)$(tput sgr0)\n";
    compile -r win-arm64;
    ;;

    7)
    printf "$(tput setaf 6)Publishing for Windows 10 (ARM 32)$(tput sgr0)\n";
    compile -r win-arm;
    ;;

    #This should only run if the user entered an invalid number...
    *)
    printf "$(tput setaf 1)Invalid selection; Aborting.$(tput sgr0)\n";
    ;;
esac

#I put this unalias-command here just in case. Is this even necessary?
unalias compile;

#remove debug files
rm -f build/*.pdb

#add a file-extension to the Linux executable
mv -f build/FileFinder build/FileFinder.x86-64 2>/dev/null

#Exit the script
exit;