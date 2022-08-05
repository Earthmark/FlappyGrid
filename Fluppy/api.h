#pragma once

#include <cstdint>

#include "absl/types/span.h"

typedef int32_t ref_handle;
typedef int32_t exec_handle;
typedef int32_t exec_result_handle;

extern "C" struct buffer_span
{
  absl::Span<uint8_t> to_span()
  {
    return absl::MakeSpan(data, length);
  }

  uint8_t* data;
  int32_t length;
};


extern "C" struct execution_result {
  exec_handle execution_handle;
  ref_handle code_handle;
  uint64_t start_nanos;
  uint64_t end_nanos;
  int32_t status_code;
};

extern "C" struct execution_result_span
{
  static execution_result_span make(absl::Span<const execution_result> spa)
  {
    return execution_result_span {
      .data = spa.data(),
      .length = static_cast<int32_t>(spa.length())
    };
  }

  const execution_result* data;
  int32_t length;
};
