#include "api.h"

#include <chrono>
#include <vector>

#include "absl/types/span.h"

#include "Noise_generated.h"

#include "handle_storage.h"
#include "operators.h"

handle_storage<absl::Span<uint8_t>> res_storage;
std::vector<execution_result> job_storage;
// The vector jobs are currently being parsed from.
std::vector<execution_result> reading_job_storage;

extern "C" __declspec(dllexport) ref_handle add_ref(uint8_t * data, int32_t length)
{
  const auto [id, alloc] = res_storage.allocate();
  *alloc = absl::MakeSpan(data, length);
  return id;
}

uint64_t current_nanos()
{
  static auto start_point = std::chrono::high_resolution_clock::now();
  return std::chrono::duration_cast<std::chrono::nanoseconds>(
    std::chrono::high_resolution_clock::now() - start_point).count();
}

extern "C" __declspec(dllexport) exec_handle execute(ref_handle handle)
{
  const absl::Span<uint8_t>* buffer = res_storage.get(handle);
  if (!buffer)
  {
    return -1;
  }

  if (flatbuffers::Verifier verifier(buffer->data(), buffer->length());
    !Noise::VerifyPatternBuffer(verifier))
  {
    return -2;
  }

  uint64_t start_nanos = current_nanos();

  const Noise::Pattern* pattern = Noise::GetPattern(buffer->data());

  operators ops(pattern, res_storage);
  int32_t status_code = ops.execute();

  if(const flatbuffers::Vector<uint32_t>* to_free = pattern->free(); to_free)
  {
    for (const ref_handle to_free : *to_free)
    {
      res_storage.free(to_free);
    }
  }
  if(!pattern->do_not_free_self())
  {
    res_storage.free(handle);
  }

  uint64_t end_nanos = current_nanos();
  int32_t exec_id = job_storage.size();

  job_storage.emplace_back(execution_result {
    .execution_handle = exec_id,
    .code_handle = handle,
    .start_nanos = start_nanos,
    .end_nanos = end_nanos,
    .status_code = status_code,
    });

  return exec_id;
}

extern "C" __declspec(dllexport) void execution_results(execution_result ** results, int32_t * length)
{
  // Stomp existing read jobs.
  reading_job_storage = std::move(job_storage);
  *results = reading_job_storage.data();
  *length = reading_job_storage.size();
}
