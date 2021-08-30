#!/bin/bash

echo -e "Extracting release version...\n"

if ! [[ ${GITHUB_REF#refs/heads/release/} =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] 
then
  echo "Invalid branch name format!" >&2 && exit 1
fi

branch_version=${GITHUB_REF#refs/heads/release/}
echo "Branch version:  $branch_version"

latest_tag=$(git describe --tags --match v*)
if [ -z $latest_tag ]
then
  echo "Tag version could not be found." >&2 && exit 1
fi

[[ $latest_tag =~ (^v[0-9]+\.[0-9]+\.[0-9]+) ]] && tag_version=${BASH_REMATCH[1]:1}
if [ -z $tag_version ]
then
  echo "Latest tag was in an unexpected format: $latest_tag" >&2 && exit 1
fi

echo "Tag version:     $tag_version"

IFS="." read -ra branch_version_parts <<< "$branch_version"
IFS="." read -ra tag_version_parts <<< "$tag_version"

if [[ ${#branch_version_parts[@]} -lt 3 || ${#tag_version_parts[@]} -lt 3 ]]
then
  echo "Error splitting the versions into their major/minor/patch versions!" >&2 && exit 1
fi

if (( ${branch_version_parts[0]} < ${tag_version_parts[0]} )) || (( ${branch_version_parts[0]} == ${tag_version_parts[0]} && ${branch_version_parts[1]} < ${tag_version_parts[1]} ))
then
  echo "Cannot release an older version than the latest release version!" >&2 && exit 1
fi

final_version=""
if (( ${branch_version_parts[0]} > ${tag_version_parts[0]} )) || (( ${branch_version_parts[0]} == ${tag_version_parts[0]} && ${branch_version_parts[1]} > ${tag_version_parts[1]} ))
then
  final_version=$branch_version
else
  if (( ${branch_version_parts[0]} == ${tag_version_parts[0]} && ${branch_version_parts[1]} == ${tag_version_parts[1]} ))
  then
    final_version="${branch_version_parts[0]}.${branch_version_parts[1]}.$((tag_version_parts[2] + 1))"
  fi
fi

echo -e "\nRelease version: $final_version"
echo "##[set-output name=version;]$(echo $final_version)"