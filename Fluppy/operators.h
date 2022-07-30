#pragma once

#include "absl/types/span.h"

#include "Noise_generated.h"
#include "handle_storage.h"

class operators
{
public:
  explicit operators(
    const Noise::Pattern* pattern,
    handle_storage<absl::Span<uint8_t>>& storage
  ) : pattern_(pattern), storage_(storage) {}

  int32_t execute();

private:
  const Noise::Pattern* pattern_;
  handle_storage<absl::Span<uint8_t>>& storage_;
};
