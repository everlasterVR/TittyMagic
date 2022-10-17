#!/bin/bash

creator_name=everlaster
resource_name=TittyMagic
package_version=$1
plugin_version=$(git tag --points-at HEAD)

[ -z "$package_version" ] && printf "Usage: ./package.sh [var package version]\n" && exit 1
[ -z "$plugin_version" ] && printf "Git tag not set on current commit.\n" && exit 1

# setup archive contents
work_dir=publish
mkdir -p work_dir

resource_dir=$work_dir/Custom/Scripts/$creator_name/$resource_name
mkdir -p $resource_dir
cp meta.json $work_dir/
cp *.cslist $resource_dir/
cp -r src $resource_dir/

# additional packaging
mkdir -p $resource_dir/vam-collider-editor
cp -r vam-collider-editor/src $resource_dir/vam-collider-editor
cp -r vam-collider-editor/*.md $resource_dir/vam-collider-editor

morphs_dir=$work_dir/Custom/Atom/Person/Morphs/female/$creator_name/$resource_name
mkdir -p $morphs_dir
cp -r local/Morphs/* $morphs_dir/

# update version info
sed -i "s/0\.0\.0/$plugin_version/g" $work_dir/meta.json
sed -i "s/0\.0\.0/$plugin_version/g" $resource_dir/src/Script.cs
sed -i "s/#define/\/\/#define/g" $resource_dir/src/Script.cs

# hide .cs files (plugin is loaded with .cslist)
for file in $(find $resource_dir -type f -name "*.cs"); do
    touch $file.hide
done

# zip files to .var and cleanup
printf "Creating package...\n"
package_file="$creator_name.$resource_name.$package_version.var"
cd $work_dir
zip -rq $package_file *
printf "Package $package_file created for plugin version $plugin_version.\n"
mv $package_file ..
cd ..
rm -rf $work_dir

# upload
if ../scripts/mega-login.sh && ../scripts/mega-uploadandshare.sh $package_file; then
    printf "Upload successful.\n"
else
    printf "Upload failed.\n"
fi;

# move archive to AddonPackages
addon_packages_dir=../../../../AddonPackages/Self
mkdir -p $addon_packages_dir
mv $package_file $addon_packages_dir
printf "Package moved to AddonPackages/Self.\n".
