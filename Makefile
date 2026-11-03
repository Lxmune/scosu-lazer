# Shortcuts for building scosu from WSL. Everything builds the working tree (uncommitted changes included)
# in ../.lazer-build/src with the Linux .NET SDK there; see build-env.sh.
#
#   make check                                         Debug build of osu.Desktop.slnf, warnings as errors (like CI)
#   make test [TESTS=osu.Game.Tests] [FILTER=TestSceneMainMenu]
#   make portable [VERSION=2026.1102.3]                portable win-x64 zip (default version: newest tag)

SHELL := bash
.SHELLFLAGS := -euo pipefail -c

TESTS ?= osu.Desktop.slnf
FILTER ?=
VERSION ?=

env := source ./build-env.sh

.PHONY: help check test portable

help:
	@sed -n 's/^#   //p' Makefile

check:
	@$(env) && sync_sources && cd "$$work/src" && "$$dotnet" build -c Debug -warnaserror osu.Desktop.slnf "$${dotnet_flags[@]}"

test: check
	@$(env) && cd "$$work/src" && "$$dotnet" test $(TESTS) --no-build $(if $(FILTER),--filter "$(FILTER)") -- NUnit.ConsoleOut=0

portable:
	@./build-portable.sh $(VERSION)
