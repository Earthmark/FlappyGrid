#pragma once

#include "absl/types/span.h"

#include "Noise_generated.h"

class operators
{
public:
  explicit operators(
    const Noise::Pattern* pattern
  ) : pattern_(pattern) {}

  int32_t execute();

private:
  const Noise::Pattern* pattern_;
};
