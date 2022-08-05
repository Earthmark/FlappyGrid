#pragma once

#include <vector>

#include "api.h"

#include "absl/types/span.h"

#include "Noise_generated.h"

#include "handle_storage.h"
#include "operators.h"

class job_processor
{
public:
  job_processor()
  {
    job_storage_.reserve(1024);
    reading_job_storage_.reserve(1024);
  }

  exec_handle execute(const absl::Span<uint8_t> span);

  absl::Span<const execution_result> consume_results();

private:
  std::vector<execution_result> job_storage_;
  // The vector jobs are currently being parsed from.
  std::vector<execution_result> reading_job_storage_;
};
