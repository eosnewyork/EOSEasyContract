#include "hello.hpp"

ACTION hello::hi(name user)
{
	print("Hello, ", name{user});
}

// EOSIO_DISPATCH(hello, (hi))
