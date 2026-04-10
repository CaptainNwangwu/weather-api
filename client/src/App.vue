<template>
  <v-app :style="bgStyle">
    <v-container class="py-8">
      <!-- Header -->
      <div class="d-flex align-center justify-space-between mb-6">
        <div style="width: 40px" />
        <div class="text-center flex-grow-1">
          <v-icon icon="mdi-weather-partly-cloudy" size="48" color="white" class="mb-2" />
          <h1 class="text-h3 font-weight-bold" style="color: white; text-shadow: 0 2px 8px rgba(0,0,0,0.2)">
            Weather Forecast
          </h1>
          <p class="text-subtitle-1 mt-1" style="color: rgba(255,255,255,0.85)">
            Powered by Visual Crossing
          </p>
        </div>
        <div class="d-flex align-center ga-1">
          <v-icon icon="mdi-weather-sunny" color="white" size="18" />
          <v-switch
            :model-value="isDark"
            @update:model-value="val => theme.change(val ? 'dark' : 'light')"
            color="blue-lighten-2"
            hide-details
            inset
            density="compact"
          />
          <v-icon icon="mdi-weather-night" color="white" size="18" />
        </div>
      </div>

      <!-- Main Card -->
      <v-card class="mx-auto" max-width="680" elevation="12" rounded="xl">
        <!-- Tab Bar -->
        <v-tabs v-model="activeTab" bg-color="blue-lighten-2" color="white" grow>
          <v-tab value="single">
            <v-icon start icon="mdi-map-marker" />
            Single Location
          </v-tab>
          <v-tab value="multi">
            <v-icon start icon="mdi-map-marker-multiple" />
            Multiple Locations
          </v-tab>
        </v-tabs>

        <v-card-text class="pa-6">
          <v-tabs-window v-model="activeTab">

            <!-- ── Single Location Tab ── -->
            <v-tabs-window-item value="single">
              <v-form @submit.prevent="fetchSingle">

                <v-text-field
                  v-model="single.location"
                  class="mt-4 mb-3"
                  label="Location *"
                  hint="City, address, ZIP code, or lat,long"
                  placeholder="e.g. Phoenix,USA or 33.4484,-112.0742"
                  prepend-inner-icon="mdi-map-search"
                  variant="outlined"
                  required
                />

                <v-row>
                  <v-col cols="12" sm="6">
                    <v-text-field
                      v-model="single.date1"
                      label="Start Date"
                      type="date"
                      hint="Optional — yyyy-MM-dd"
                      prepend-inner-icon="mdi-calendar-start"
                      variant="outlined"
                      clearable
                    />
                  </v-col>
                  <v-col cols="12" sm="6">
                    <v-text-field
                      v-model="single.date2"
                      label="End Date"
                      type="date"
                      hint="Requires start date"
                      prepend-inner-icon="mdi-calendar-end"
                      variant="outlined"
                      clearable
                      :disabled="!single.date1"
                    />
                  </v-col>
                </v-row>

                <v-select
                  v-model="single.include"
                  :items="includeOptions"
                  label="Include Sections"
                  hint="Leave empty to return all sections"
                  prepend-inner-icon="mdi-filter-variant"
                  variant="outlined"
                  multiple
                  chips
                  closable-chips
                  clearable
                  class="mb-3"
                  placeholder="All"
                />

                <v-select
                  v-model="single.elements"
                  :items="elementOptions"
                  label="Weather Elements"
                  hint="Leave empty for all elements; temp, conditions &amp; description always included"
                  prepend-inner-icon="mdi-thermometer"
                  variant="outlined"
                  multiple
                  chips
                  closable-chips
                  clearable
                  class="mb-5"
                  placeholder="All"
                />

                <v-alert
                  v-if="singleError"
                  type="error"
                  variant="tonal"
                  closable
                  class="mb-4"
                  @click:close="singleError = null"
                >
                  {{ singleError }}
                </v-alert>

                <v-btn
                  type="submit"
                  :color="isDark ? 'blue-lighten-2' : 'blue-darken-2'"
                  size="large"
                  block
                  rounded="lg"
                  :loading="singleLoading"
                  :disabled="!single.location"
                >
                  <v-icon start icon="mdi-cloud-search" />
                  Get Weather
                </v-btn>
              </v-form>
            </v-tabs-window-item>

            <!-- ── Multiple Locations Tab ── -->
            <v-tabs-window-item value="multi">
              <v-form @submit.prevent="fetchMulti">

                <v-text-field
                  v-model="multi.locations"
                  class="mt-4 mb-3"
                  label="Locations *"
                  hint="Separate multiple locations with a pipe `|` symbol"
                  placeholder="e.g. Phoenix,USA|London,UK|Hamburg,Germany"
                  prepend-inner-icon="mdi-map-marker-multiple"
                  variant="outlined"
                  required
                />

                <v-row>
                  <v-col cols="12" sm="6">
                    <v-text-field
                      v-model="multi.datestart"
                      label="Start Date"
                      type="date"
                      hint="Optional — yyyy-MM-dd"
                      prepend-inner-icon="mdi-calendar-start"
                      variant="outlined"
                      clearable
                    />
                  </v-col>
                  <v-col cols="12" sm="6">
                    <v-text-field
                      v-model="multi.dateend"
                      label="End Date"
                      type="date"
                      hint="Requires start date"
                      prepend-inner-icon="mdi-calendar-end"
                      variant="outlined"
                      clearable
                      :disabled="!multi.datestart"
                    />
                  </v-col>
                </v-row>

                <v-select
                  v-model="multi.include"
                  :items="includeOptions"
                  label="Include Sections"
                  hint="Leave empty to return all sections"
                  prepend-inner-icon="mdi-filter-variant"
                  variant="outlined"
                  multiple
                  chips
                  closable-chips
                  clearable
                  class="mb-3"
                  placeholder="All"
                />

                <v-select
                  v-model="multi.elements"
                  :items="elementOptions"
                  label="Weather Elements"
                  hint="Leave empty for all elements; temp, conditions &amp; description always included"
                  prepend-inner-icon="mdi-thermometer"
                  variant="outlined"
                  multiple
                  chips
                  closable-chips
                  clearable
                  class="mb-5"
                  placeholder="All"
                />

                <v-alert
                  v-if="multiError"
                  type="error"
                  variant="tonal"
                  closable
                  class="mb-4"
                  @click:close="multiError = null"
                >
                  {{ multiError }}
                </v-alert>

                <v-btn
                  type="submit"
                  :color="isDark ? 'blue-lighten-2' : 'blue-darken-2'"
                  size="large"
                  block
                  rounded="lg"
                  :loading="multiLoading"
                  :disabled="!multi.locations"
                >
                  <v-icon start icon="mdi-cloud-search" />
                  Get Weather
                </v-btn>
              </v-form>
            </v-tabs-window-item>

          </v-tabs-window>
        </v-card-text>
      </v-card>

      <!-- Results -->
      <v-expand-transition>
        <WeatherResults
          v-if="results"
          :raw-data="results.data"
          :cached="results.cached"
          class="mt-6"
          @close="results = null"
        />
      </v-expand-transition>
    </v-container>
  </v-app>
</template>

<script setup>
import { ref, reactive, computed } from 'vue'

const ALWAYS_INCLUDE = ['icon', 'temp', 'tempmax', 'tempmin', 'conditions', 'description']
import { useTheme } from 'vuetify'
import { API_BASE_URL } from './config.js'
import WeatherResults from './components/WeatherResults.vue'

const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)
function toggleDark() {
  theme.change(isDark.value ? 'light' : 'dark')
}

const bgStyle = computed(() =>
  isDark.value
    ? `background:
        radial-gradient(ellipse 130% 55% at 50% -5%, rgba(30, 70, 150, 0.32) 0%, transparent 100%),
        linear-gradient(180deg, #060d1a 0%, #080e1e 55%, #050b16 100%);
       min-height: 100vh`
    : 'background: linear-gradient(160deg, #87CEEB 0%, #B8E4F9 50%, #E0F6FF 100%); min-height: 100vh'
)

const activeTab = ref('single')
const results = ref(null)

// Single location form state
const single = reactive({ location: '', date1: '', date2: '', include: [], elements: [] })
const singleLoading = ref(false)
const singleError = ref(null)

// Multi location form state
const multi = reactive({ locations: '', datestart: '', dateend: '', include: [], elements: [] })
const multiLoading = ref(false)
const multiError = ref(null)

const includeOptions = [
  { title: 'Days', value: 'days' },
  { title: 'Hours', value: 'hours' },
  { title: 'Minutes (beta)', value: 'minutes' },
  { title: 'Alerts', value: 'alerts' },
  { title: 'Current Conditions', value: 'current' },
  { title: 'Events', value: 'events' },
  { title: 'Historical Observations', value: 'obs' },
  { title: 'Remote (Satellite/Radar)', value: 'remote' },
  { title: 'Forecast', value: 'fcst' },
  { title: 'Statistics', value: 'stats' },
  { title: 'Statistical Forecast', value: 'statsfcst' },
]

const elementOptions = [
  { title: 'Feels Like', value: 'feelslike' },
  { title: 'Humidity', value: 'humidity' },
  { title: 'Precipitation', value: 'precip' },
  { title: 'Precip Probability', value: 'precipprob' },
  { title: 'Precip Type', value: 'preciptype' },
  { title: 'Wind Speed', value: 'windspeed' },
  { title: 'Wind Gust', value: 'windgust' },
  { title: 'Wind Direction', value: 'winddir' },
  { title: 'Pressure', value: 'pressure' },
  { title: 'Cloud Cover', value: 'cloudcover' },
  { title: 'Visibility', value: 'visibility' },
  { title: 'Solar Radiation', value: 'solarradiation' },
  { title: 'UV Index', value: 'uvindex' },
  { title: 'Snow Depth', value: 'snowdepth' },
  { title: 'Sunrise', value: 'sunrise' },
  { title: 'Sunset', value: 'sunset' },
]

async function fetchSingle() {
  singleLoading.value = true
  singleError.value = null
  results.value = null

  try {
    const params = new URLSearchParams()
    params.append('location', single.location)
    if (single.date1) params.append('date1', single.date1)
    if (single.date2 && single.date1) params.append('date2', single.date2)
    if (single.include.length) params.append('include', single.include.join(','))
    if (single.elements.length) {
      const elems = [...new Set([...single.elements, ...ALWAYS_INCLUDE])]
      params.append('elements', elems.join(','))
    }

    const response = await fetch(`${API_BASE_URL}/api/weather?${params}`)
    if (!response.ok) {
      const data = await response.json().catch(() => ({}))
      throw new Error(data.error || `Request failed (${response.status})`)
    }
    results.value = { data: await response.json(), cached: response.headers.get('X-Cache-Status') === 'HIT' }
  } catch (e) {
    singleError.value = e.message || 'An unexpected error occurred'
  } finally {
    singleLoading.value = false
  }
}

async function fetchMulti() {
  multiLoading.value = true
  multiError.value = null
  results.value = null

  try {
    const params = new URLSearchParams()
    params.append('locations', multi.locations)
    if (multi.datestart) params.append('datestart', multi.datestart)
    if (multi.dateend && multi.datestart) params.append('dateend', multi.dateend)
    if (multi.include.length) params.append('include', multi.include.join(','))
    if (multi.elements.length) {
      const elems = [...new Set([...multi.elements, ...ALWAYS_INCLUDE])]
      params.append('elements', elems.join(','))
    }

    const response = await fetch(`${API_BASE_URL}/api/weather/multi?${params}`)
    if (!response.ok) {
      const data = await response.json().catch(() => ({}))
      throw new Error(data.error || `Request failed (${response.status})`)
    }
    results.value = { data: await response.json(), cached: response.headers.get('X-Cache-Status') === 'HIT' }
  } catch (e) {
    multiError.value = e.message || 'An unexpected error occurred'
  } finally {
    multiLoading.value = false
  }
}
</script>
