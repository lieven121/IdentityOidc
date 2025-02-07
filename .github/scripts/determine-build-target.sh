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

  elif [ "$BRANCH_NAME" == "release*" ]; then
    # Ensure that a tag is present
    if git diff-tree --no-commit-id --name-only -r HEAD | grep -q '^VERSION'; then
      echo "VERSION file was updated in this commit."
      VERSION=$(cat VERSION)
      IMAGE_TAG='type=raw,value='${VERSION}'-rc,enable=true'
      IMAGE_TAG_ALT='type=raw,value=latest-rc,enable=true'
      echo "RC branch detected. Using image tag: ${IMAGE_TAG}"
    else
      echo "ERROR: VERSION file not updated on RC branch or no tag present. Aborting build."
      exit 1
    fi

  elif [ "$BRANCH_NAME" == "master" ]; then
    # Ensure that the VERSION file was updated and a tag is present
    if git diff-tree --no-commit-id --name-only -r HEAD | grep -q '^VERSION'; then
      echo "VERSION file was updated in this commit."
      VERSION=$(cat VERSION)
      IMAGE_TAG='type=raw,value='${VERSION}',enable=true'
      IMAGE_TAG_ALT='type=raw,value=latest,enable=true'
      echo "Release branch detected. Using image tag: ${IMAGE_TAG}"
    else
      echo "ERROR: VERSION file not updated on release branch or no tag present. Aborting build."
      exit 1
    fi
  else
    echo "Branch ${BRANCH_NAME} does not have a defined build strategy. Aborting."
    exit 1
  fi

# Export the IMAGE_TAG for later steps
echo "IMAGE_TAG=${IMAGE_TAG}" >> $GITHUB_OUTPUT
echo "IMAGE_TAG_ALT=${IMAGE_TAG_ALT}" >> $GITHUB_OUTPUT