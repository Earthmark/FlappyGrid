#pragma once

#include "absl/container/flat_hash_map.h"

template<typename T>
class handle_storage
{
public:
  using handle_type = uint32_t;

  std::pair<handle_type, T*> allocate()
  {
    uint32_t handle = next_free_++;
    T* target = &buffer_[handle];
    return std::make_pair(handle, target);
  }

  void free(handle_type handle)
  {
    buffer_.erase(handle);
  }

  T* get(handle_type handle)
  {
    if (auto found = buffer_.find(handle); found != buffer_.end())
    {
      return &found->second;
    }
    return nullptr;
  }

private:
  uint32_t next_free_ = 0;
  absl::flat_hash_map<handle_type, T> buffer_;
};