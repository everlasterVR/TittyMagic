#!/bin/bash
resource_name=${PWD##*/}
resource_version=$1
package_version=$2

if [ -z "$resource_version" ]; then
    echo "Resource version must be set."
    exit 1
fi

if [ -z "$package_version" ]; then
    echo "Package version must be set."
    exit 1
fi

# Setup archive contents
resource_dir=publish/Custom/Scripts/everlaster/$resource_name/
mkdir -p $resource_dir
cp *.cslist $resource_dir
cp -r src/ $resource_dir

morphs_dir=publish/Custom/Atom/Person/Morphs/female/everlaster/
mkdir -p $morphs_dir
cp -r /mnt/e/VaM/Custom/Atom/Person/Morphs/female/everlaster/$resource_name $morphs_dir

# Update version info in meta.json
cp meta.json publish/
sed -i "s/v0.0.0/$resource_version/" publish/meta.json

cd publish

# hide .cs files (plugin is loaded with .cslist)
for file in $(find . -type f -name "*.cs"); do
    touch $file.hide
done

# Zip files to .var and cleanup
package="everlaster.$resource_name.$package_version.var"
zip -r $package *
cd ..
mv publish/*.var .
rm -r publish/*

echo "Package $package created."
