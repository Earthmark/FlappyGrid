#include "job_processor.h"

#include "Noise_generated.h"

#include <chrono>

uint64_t current_nanos()
{
  return 0;
  static auto start_point = std::chrono::high_resolution_clock::now();
  return std::chrono::duration_cast<std::chrono::nanoseconds>(
    std::chrono::high_resolution_clock::now() - start_point).count();
}


int job_processor::execute_internal(const absl::Span<uint8_t> span)
{
  if (flatbuffers::Verifier verifier(span.data(), span.length());
    !Noise::VerifyPatternBuffer(verifier))
  {
    return -1;
  }

  const Noise::Pattern* pattern = Noise::GetPattern(span.data());

  operators ops(pattern);
  return ops.execute();
}

exec_handle job_processor::execute(const absl::Span<uint8_t> span)
{
  uint64_t start_nanos = current_nanos();
  int status = execute_internal(span);
  uint64_t end_nanos = current_nanos();

  int32_t exec_id = ++last_id_;
  job_storage_.emplace_back(execution_result{
    .execution_handle = exec_id,
    .start_nanos = start_nanos,
    .end_nanos = end_nanos,
    .status_code = status, //status_code,
    });

  return exec_id;
}

absl::Span<const execution_result> job_processor::consume_results()
{
  reading_job_storage_.clear();
  std::swap(reading_job_storage_, job_storage_);
  return absl::MakeConstSpan(reading_job_storage_);
}
