VERSION = $(shell awk '/^v[0-9]/ {print substr($$1, 2); exit }' CHANGELOG.md)
TARGET = ArmatureCleanup-v$(VERSION).unitypackage

all: build

version:
	@echo $(VERSION)

Editor/Version.cs: CHANGELOG.md
	@sed -i 's/VERSION = ".*"/VERSION = "v$(VERSION)"/' $@

package.json: .package.json.tmpl CHANGELOG.md
	env VERSION=$(VERSION) envsubst < $< > $@

$(TARGET): Editor/Version.cs package.json
	# copy stuff to a tempdir to build our release tree
	mkdir -p .tmp/Assets/SophieBlue/ArmatureCleanup
	ls | grep -v "Assets" | xargs -i{} cp -a {} .tmp/Assets/SophieBlue/ArmatureCleanup/
	.github/workflows/generate_meta.sh bc846a2331c27846b961e0f9fe107d54 > .tmp/Assets/SophieBlue.meta
	.github/workflows/generate_meta.sh 0124009027f4c92f68cc6df60787ce3e > .tmp/Assets/SophieBlue/ArmatureCleanup.meta

	# build the unity package
	cup -c 2 -o $@ -s .tmp
	mv .tmp/$@ .
	rm -rf .tmp

	# rebuild the unity package
	unzip -d .tmp $@
	rm $@
	cd .tmp && tar cvf ../$@ * && cd -
	rm -rf .tmp

build: $(TARGET)

clean:
	rm -f $(TARGET)
	rm -rf .tmp
.PHONY: clean
