# Kumu
Post processing effects made for Unity

# Quick Start
## Install
1. Go to your Unity project folder
1. run `git submodule add -b stable git@github.com:SebaschenLiu/Kumu.git Assets/Plugins/Kumu` 
1. run command "git submodule init"
1. run commmand "git submodule update"

## Uninstall
1. Go to your Unity project folder
1. run `git submodule deinit -f -- Assets/Plugins/TP   ` 
1. run `rm -rf .git/modules/Assets/Plugins/TP`
1. run `git rm -f Assets/Plugins/TP`

## FAQ
* Why doesn't this submodule go away when I checkout older commits?
	run `git clean -d -f -f`. Git by default refuses to delete directories with .git, so the second -f will remove the submodule folder.

# Guide
## Dependencies

All dependencies/plugins are placed in the Plugins folder

## Issues
If git submodules don't work well I'll make Unity packages for the project
