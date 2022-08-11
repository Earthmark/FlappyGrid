cmake_minimum_required (VERSION 3.19)

project("Fluppy")

include(FetchContent)

FetchContent_Declare(
        absl-cpp
        GIT_REPOSITORY https://github.com/abseil/abseil-cpp.git
        GIT_TAG        215105818dfde3174fe799600bb0f3cae233d0bf # 20211102.0
)
set(ABSL_PROPAGATE_CXX_STD ON)
FetchContent_MakeAvailable(absl-cpp)

FetchContent_Declare(
        flatbuffers
        GIT_REPOSITORY https://github.com/google/flatbuffers.git
        GIT_TAG        5f013760271a5bb9b81bb31d3e967fce1eab9295 # head?
)
FetchContent_MakeAvailable(flatbuffers)

set(NOISE_SCHEMA_PATH ${CMAKE_CURRENT_SOURCE_DIR}/../Flappy/Noise.fbs)
set(NOISE_FLATC_INCLUDE ${CMAKE_CURRENT_BINARY_DIR}/flatc_include)
set(NOISE_HEADERS ${NOISE_FLATC_INCLUDE}/Noise_generated.h)
add_custom_command(
  COMMAND $<TARGET_FILE:flatc> --cpp -o ${NOISE_FLATC_INCLUDE} ${NOISE_SCHEMA_PATH}
  DEPENDS ${NOISE_SCHEMA_PATH} flatc
  OUTPUT ${NOISE_HEADERS}
)

add_library(Fluppy SHARED
  "api.cpp" "api.h"
  ${NOISE_HEADERS}
   "handle_storage.h"
  "operators.h" "operators.cpp"
  "job_processor.h" "job_processor.cpp")

set_property(TARGET Fluppy PROPERTY CXX_STANDARD 20)

target_include_directories(Fluppy PUBLIC $<BUILD_INTERFACE:${NOISE_FLATC_INCLUDE}>)

target_link_libraries(Fluppy absl::base absl::flat_hash_map flatbuffers)

add_custom_command(TARGET Fluppy
  COMMAND ${CMAKE_COMMAND} -E copy
  $<TARGET_FILE:Fluppy>
  ${CMAKE_CURRENT_SOURCE_DIR}/../Flappy/)

add_custom_command(TARGET Fluppy
  COMMAND ${CMAKE_COMMAND} -E copy
  $<TARGET_FILE:flatc>
  ${CMAKE_CURRENT_SOURCE_DIR}/../Flappy/)
