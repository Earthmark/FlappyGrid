#pragma once

#include <cstdint>

typedef int32_t ref_handle;
typedef int32_t exec_handle;
typedef int32_t exec_result_handle;

struct execution_result {
  exec_handle execution_handle;
  ref_handle code_handle;
  uint64_t start_nanos;
  uint64_t end_nanos;
  int32_t status_code;
};
