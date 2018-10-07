cmake_minimum_required(VERSION 3.5)
project(test_example VERSION 1.0.0)

if(EOSIO_CDT_ROOT STREQUAL "" OR NOT EOSIO_CDT_ROOT)
	    set(EOSIO_CDT_ROOT "/usr/local/eosio.cdt")
endif()
list(APPEND CMAKE_MODULE_PATH ${EOSIO_CDT_ROOT}/lib/cmake)
include(EosioWasmToolchain)

add_executable( test.wasm test.cpp )
