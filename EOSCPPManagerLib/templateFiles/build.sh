#! /bin/bash

printf "\n\n"
printf "\t=========== BEGIN: Building Contract ===========\n\n"

RED='\033[0;31m'
NC='\033[0m'

CORES=`getconf _NPROCESSORS_ONLN`
mkdir -p build
pushd build &> /dev/null
cmake ../
make -j${CORES}
popd &> /dev/null

printf "\t=========== END: Building Contract ===========\n\n"
printf "\n\n"