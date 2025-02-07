#!/bin/bash

IMAGE_TAG=''
IMAGE_TAG_ALT=''
# Extract branch name from GITHUB_REF (e.g. refs/heads/dev becomes dev)
BRANCH_NAME="${GITHUB_REF#refs/heads/}"
if [ "$BRANCH_NAME" == "dev" ]; then
  DATE_TAG=$(date +%Y-%m-%d-%H-%M)
  IMAGE_TAG='type=raw,value=dev-'${DATE_TAG}',enable=true'
  IMAGE_TAG_ALT='type=raw,value=dev,enable=true'
  echo "Dev branch detected. Using image tag: ${IMAGE_TAG}"

elif [ "$BRANCH_NAME" == "release" ]; then
  # Extract version from the tag
  VERSION=$(git describe --tags --abbrev=0)
  IMAGE_TAG='type=raw,value='${VERSION}'-rc,enable=true'
  IMAGE_TAG_ALT='type=raw,value=latest-rc,enable=true'
  echo "RC branch detected. Using image tag: ${IMAGE_TAG}"

elif [ "$BRANCH_NAME" == "main" ]; then
  # Extract version from the tag
  VERSION=$(git describe --tags --abbrev=0)
  IMAGE_TAG='type=raw,value='${VERSION}',enable=true'
  IMAGE_TAG_ALT='type=raw,value=latest,enable=true'
  echo "Release branch detected. Using image tag: ${IMAGE_TAG}"
else
  echo "Branch ${BRANCH_NAME} does not have a defined build strategy. Aborting."
  exit 1
fi

# Export the IMAGE_TAG for later steps
echo "IMAGE_TAG=${IMAGE_TAG}" >> $GITHUB_OUTPUT
echo "IMAGE_TAG_ALT=${IMAGE_TAG_ALT}" >> $GITHUB_OUTPUT