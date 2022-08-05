#include "job_processor.h"

#include "Noise_generated.h"

#include <chrono>

uint64_t current_nanos()
{
  static auto start_point = std::chrono::high_resolution_clock::now();
  return std::chrono::duration_cast<std::chrono::nanoseconds>(
    std::chrono::high_resolution_clock::now() - start_point).count();
}

exec_handle job_processor::execute(const absl::Span<uint8_t> span)
{
  //if (flatbuffers::Verifier verifier(buffer->data(), buffer->length());
  //  !Noise::VerifyPatternBuffer(verifier))
  //{
  //  return -2;
  //}

  //uint64_t start_nanos = current_nanos();

  const Noise::Pattern* pattern = Noise::GetPattern(span.data());

  //operators ops(pattern, res_storage_);
  //int32_t status_code = ops.execute();

  //if (const flatbuffers::Vector<uint32_t>* to_free = pattern->free(); to_free)
  //{
  //  for (const ref_handle to_free : *to_free)
  //  {
  //    res_storage_.free(to_free);
  //  }
  //}
  //if (!pattern->do_not_free_self())
   
  //uint64_t end_nanos = current_nanos();
  int32_t exec_id = job_storage_.size();

  job_storage_.emplace_back(execution_result{
    .execution_handle = exec_id,
    //.start_nanos = start_nanos,
    //.end_nanos = end_nanos,
    //.status_code = status_code,
    });

  return exec_id;
}

absl::Span<const execution_result> job_processor::consume_results()
{
  reading_job_storage_.clear();
  std::swap(reading_job_storage_, job_storage_);
  return absl::MakeConstSpan(reading_job_storage_);
}
