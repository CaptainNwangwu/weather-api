<template>
  <div>
    <v-card v-for="(loc, i) in pagedLocations" :key="i"
      class="mx-auto mb-6" max-width="900" elevation="8" rounded="xl">

      <!-- Location Header -->
      <v-card-title :class="['d-flex', 'align-start', 'justify-space-between', 'pa-5', isDark ? 'bg-blue-darken-3 text-white' : 'bg-blue-lighten-4']">
        <div>
          <div class="text-h6 font-weight-bold">
            <v-icon start icon="mdi-map-marker" color="blue-darken-2" />
            {{ loc.resolvedAddress || loc.address || loc._name }}
          </div>
          <div class="d-flex flex-wrap ga-2 mt-2">
            <v-tooltip v-if="loc.latitude != null" text="Coordinates" location="bottom">
              <template #activator="{ props: tip }">
                <v-chip v-bind="tip" size="small" prepend-icon="mdi-earth" variant="outlined"
                  :color="isDark ? 'blue-lighten-2' : 'blue-darken-2'">
                  {{ round(loc.latitude) }}, {{ round(loc.longitude) }}
                </v-chip>
              </template>
            </v-tooltip>
            <v-tooltip v-if="loc.timezone" text="Timezone" location="bottom">
              <template #activator="{ props: tip }">
                <v-chip v-bind="tip" size="small" prepend-icon="mdi-clock-outline" variant="outlined"
                  :color="isDark ? 'blue-lighten-2' : 'blue-darken-2'">
                  {{ loc.timezone }}
                </v-chip>
              </template>
            </v-tooltip>
            <v-tooltip v-if="loc.queryCost" text="API records consumed by this query" location="bottom">
              <template #activator="{ props: tip }">
                <v-chip v-bind="tip" size="small" prepend-icon="mdi-database-outline" variant="outlined"
                  :color="isDark ? 'grey-lighten-2' : 'grey-darken-1'">
                  {{ loc.queryCost }} records
                </v-chip>
              </template>
            </v-tooltip>
          </div>
        </div>
        <v-btn v-if="i === 0" icon="mdi-close" variant="text" density="compact" @click="$emit('close')" />
      </v-card-title>

      <!-- Current Conditions -->
      <template v-if="loc.currentConditions">
        <v-divider />
        <v-card-text class="pa-5">
          <div class="text-overline text-medium-emphasis mb-3">Current Conditions</div>
          <v-row align="center">
            <v-col cols="auto">
              <v-icon :icon="vcIcon(loc.currentConditions.icon)" size="56" color="blue-darken-1" />
            </v-col>
            <v-col cols="auto">
              <div class="text-h3 font-weight-light">{{ round(loc.currentConditions.temp) }}°</div>
              <div class="text-body-1 text-medium-emphasis">{{ loc.currentConditions.conditions }}</div>
              <div v-if="loc.currentConditions.feelslike != null" class="text-caption text-medium-emphasis">
                Feels like {{ round(loc.currentConditions.feelslike) }}°
              </div>
            </v-col>
            <v-col>
              <v-row density="comfortable">
                <v-col v-for="(val, label) in ccMeta(loc.currentConditions)" :key="label" cols="6">
                  <div class="text-caption text-medium-emphasis">{{ label }}</div>
                  <div class="text-body-2 font-weight-medium">{{ val }}</div>
                </v-col>
              </v-row>
            </v-col>
          </v-row>
        </v-card-text>
      </template>

      <!-- Days -->
      <template v-if="loc.days?.length">
        <v-divider />
        <v-card-text class="pa-4">
          <div class="text-overline text-medium-emphasis mb-2">
            {{ loc.days.length }}-Day Forecast
          </div>
          <v-expansion-panels variant="accordion">
            <v-expansion-panel v-for="day in loc.days" :key="day.datetime">

              <!-- Day Summary Row -->
              <v-expansion-panel-title>
                <v-row align="center" no-gutters class="w-100 pr-2">
                  <v-col cols="3" sm="2">
                    <span class="text-body-2 font-weight-bold">{{ fmtDate(day.datetime) }}</span>
                  </v-col>
                  <v-col cols="1">
                    <v-icon :icon="vcIcon(day.icon)" color="blue-darken-1" size="20" />
                  </v-col>
                  <v-col cols="3" sm="2">
                    <span class="text-body-2">
                      <span v-if="day.tempmax != null" class="font-weight-bold">{{ round(day.tempmax) }}°</span>
                      <span v-if="day.tempmin != null" class="text-medium-emphasis"> / {{ round(day.tempmin) }}°</span>
                      <span v-else-if="day.temp != null">{{ round(day.temp) }}°</span>
                    </span>
                  </v-col>
                  <v-col class="d-none d-sm-block text-truncate">
                    <span class="text-body-2 text-medium-emphasis">{{ day.conditions }}</span>
                  </v-col>
                  <v-col cols="auto" class="d-none d-md-flex align-center ga-3">
                    <span v-if="day.humidity != null" class="text-body-2 text-medium-emphasis">
                      <v-icon size="14" icon="mdi-water" />{{ round(day.humidity) }}%
                    </span>
                    <span v-if="day.windspeed != null" class="text-body-2 text-medium-emphasis">
                      <v-icon size="14" icon="mdi-weather-windy" />{{ round(day.windspeed) }}
                    </span>
                  </v-col>
                </v-row>
              </v-expansion-panel-title>

              <!-- Day Detail -->
              <v-expansion-panel-text>
                <v-row density="comfortable" class="mb-4">
                  <v-col v-for="(val, label) in dayMeta(day)" :key="label" cols="6" sm="4" md="3">
                    <div class="text-caption text-medium-emphasis">{{ label }}</div>
                    <div class="text-body-2 font-weight-medium">{{ val }}</div>
                  </v-col>
                </v-row>

                <!-- Hourly Table -->
                <template v-if="day.hours?.length">
                  <div class="text-caption text-medium-emphasis mb-2">Hourly Breakdown</div>
                  <div class="overflow-x-auto">
                    <table class="hours-table">
                      <thead>
                        <tr>
                          <th>Time</th>
                          <th></th>
                          <th>Temp</th>
                          <th>Feels Like</th>
                          <th>Humidity</th>
                          <th>Wind</th>
                          <th>
                            <v-tooltip text="Precipitation amount" location="top">
                              <template #activator="{ props: tip }">
                                <span v-bind="tip">Precip</span>
                              </template>
                            </v-tooltip>
                          </th>
                          <th>
                            <v-tooltip text="Probability of precipitation" location="top">
                              <template #activator="{ props: tip }">
                                <span v-bind="tip"><v-icon size="13" icon="mdi-umbrella-outline" /> %</span>
                              </template>
                            </v-tooltip>
                          </th>
                          <th>Conditions</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr v-for="h in day.hours" :key="h.datetime">
                          <td>{{ fmtTime(h.datetime) }}</td>
                          <td><v-icon :icon="vcIcon(h.icon)" size="16" color="blue-darken-1" /></td>
                          <td>{{ h.temp != null ? round(h.temp) + '°' : '—' }}</td>
                          <td>{{ h.feelslike != null ? round(h.feelslike) + '°' : '—' }}</td>
                          <td>{{ h.humidity != null ? round(h.humidity) + '%' : '—' }}</td>
                          <td>{{ h.windspeed != null ? round(h.windspeed) + ' mph' : '—' }}</td>
                          <td>{{ h.precip != null ? h.precip + '"' : '—' }}</td>
                          <td>{{ h.precipprob != null ? h.precipprob + '%' : '—' }}</td>
                          <td>{{ h.conditions || '—' }}</td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </template>
              </v-expansion-panel-text>

            </v-expansion-panel>
          </v-expansion-panels>
        </v-card-text>
      </template>

      <!-- Raw JSON toggle -->
      <v-card-actions class="px-4 pb-3">
        <v-btn variant="text" size="small" prepend-icon="mdi-code-json" @click="showRaw = !showRaw">
          {{ showRaw ? 'Hide' : 'Show' }} Raw JSON
        </v-btn>
      </v-card-actions>
      <v-expand-transition>
        <v-card-text v-if="showRaw" class="pt-0 px-4 pb-4">
          <pre class="text-body-2 bg-grey-lighten-4 pa-4 rounded overflow-auto"
            style="max-height: 400px; white-space: pre-wrap; word-break: break-all">{{ JSON.stringify(rawData, null, 2) }}</pre>
        </v-card-text>
      </v-expand-transition>

    </v-card>
  <div v-if="totalPages > 1" class="d-flex justify-center mt-2 mb-4">
    <v-pagination
      v-model="page"
      :length="totalPages"
      :total-visible="5"
      rounded="circle"
    />
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useTheme } from 'vuetify'

const props = defineProps({
  rawData: { type: Object, required: true }
})

defineEmits(['close'])

const showRaw = ref(false)

const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)

// Normalize single/multi response into a flat array
const locations = computed(() => {
  if (!props.rawData) return []
  if (props.rawData.locations && typeof props.rawData.locations === 'object') {
    return Object.entries(props.rawData.locations).map(([name, data]) => ({ _name: name, ...data }))
  }
  return [props.rawData]
})

const PAGE_SIZE = 3
const page = ref(1)
const totalPages = computed(() => Math.ceil(locations.value.length / PAGE_SIZE))
const pagedLocations = computed(() => {
  const start = (page.value - 1) * PAGE_SIZE
  return locations.value.slice(start, start + PAGE_SIZE)
})

watch(() => props.rawData, () => { page.value = 1 })

// ── Icon mapping ──────────────────────────────────────────────

const VC_ICONS = {
  'clear-day':             'mdi-weather-sunny',
  'clear-night':           'mdi-weather-night',
  'partly-cloudy-day':     'mdi-weather-partly-cloudy',
  'partly-cloudy-night':   'mdi-weather-night-partly-cloudy',
  'cloudy':                'mdi-weather-cloudy',
  'rain':                  'mdi-weather-rainy',
  'showers-day':           'mdi-weather-partly-rainy',
  'showers-night':         'mdi-weather-partly-rainy',
  'thunder-rain':          'mdi-weather-lightning-rainy',
  'thunder-showers-day':   'mdi-weather-lightning-rainy',
  'thunder-showers-night': 'mdi-weather-lightning-rainy',
  'snow':                  'mdi-weather-snowy',
  'snow-showers-day':      'mdi-weather-snowy',
  'snow-showers-night':    'mdi-weather-snowy',
  'sleet':                 'mdi-weather-snowy-rainy',
  'wind':                  'mdi-weather-windy',
  'fog':                   'mdi-weather-fog',
  'hail':                  'mdi-weather-hail',
}

function vcIcon(name) {
  return VC_ICONS[name] ?? 'mdi-weather-cloudy'
}

// ── Helpers ───────────────────────────────────────────────────

function round(val) {
  if (val == null) return null
  return typeof val === 'number' ? Math.round(val * 10) / 10 : val
}

function fmtDate(datetime) {
  if (!datetime) return ''
  return new Date(datetime + 'T12:00:00').toLocaleDateString('en-US', {
    weekday: 'short', month: 'short', day: 'numeric'
  })
}

function fmtTime(time) {
  if (!time) return ''
  const [hStr, mStr] = time.split(':')
  const h = parseInt(hStr)
  return `${h % 12 || 12}:${mStr} ${h >= 12 ? 'PM' : 'AM'}`
}

function windDirLabel(deg) {
  if (deg == null) return '—'
  const dirs = ['N','NNE','NE','ENE','E','ESE','SE','SSE','S','SSW','SW','WSW','W','WNW','NW','NNW']
  return dirs[Math.round(deg / 22.5) % 16]
}

function ccMeta(cc) {
  const d = {}
  if (cc.humidity   != null) d['Humidity']    = round(cc.humidity) + '%'
  if (cc.windspeed  != null) d['Wind Speed']  = round(cc.windspeed) + ' mph'
  if (cc.winddir    != null) d['Wind Dir']    = windDirLabel(cc.winddir)
  if (cc.pressure   != null) d['Pressure']    = round(cc.pressure) + ' mb'
  if (cc.cloudcover != null) d['Cloud Cover'] = round(cc.cloudcover) + '%'
  if (cc.visibility != null) d['Visibility']  = round(cc.visibility) + ' mi'
  if (cc.uvindex    != null) d['UV Index']    = cc.uvindex
  if (cc.dewpoint   != null) d['Dew Point']   = round(cc.dewpoint) + '°'
  return d
}

function dayMeta(day) {
  const d = {}
  if (day.feelslikemax != null || day.feelslikemin != null)
    d['Feels Like']  = `${round(day.feelslikemax) ?? '?'}° / ${round(day.feelslikemin) ?? '?'}°`
  else if (day.feelslike != null)
    d['Feels Like']  = round(day.feelslike) + '°'
  if (day.precip     != null) d['Precip']      = day.precip + '"'
  if (day.precipprob != null) d['Precip Prob'] = day.precipprob + '%'
  if (day.preciptype != null) d['Precip Type'] = Array.isArray(day.preciptype) ? day.preciptype.join(', ') : day.preciptype
  if (day.windspeed  != null) d['Wind Speed']  = round(day.windspeed) + ' mph'
  if (day.windgust   != null) d['Wind Gust']   = round(day.windgust) + ' mph'
  if (day.winddir    != null) d['Wind Dir']    = windDirLabel(day.winddir)
  if (day.pressure   != null) d['Pressure']    = round(day.pressure) + ' mb'
  if (day.cloudcover != null) d['Cloud Cover'] = round(day.cloudcover) + '%'
  if (day.visibility != null) d['Visibility']  = round(day.visibility) + ' mi'
  if (day.uvindex    != null) d['UV Index']    = day.uvindex
  if (day.snowdepth  != null) d['Snow Depth']  = day.snowdepth + '"'
  if (day.sunrise    != null) d['Sunrise']     = day.sunrise
  if (day.sunset     != null) d['Sunset']      = day.sunset
  if (day.description)        d['Description'] = day.description
  return d
}
</script>

<style scoped>
.hours-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.8rem;
}
.hours-table th,
.hours-table td {
  padding: 6px 12px;
  text-align: left;
  border-bottom: 1px solid rgba(128, 128, 128, 0.2);
  white-space: nowrap;
}
.hours-table th {
  font-weight: 600;
  font-size: 0.72rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  opacity: 0.6;
}
.hours-table tr:last-child td {
  border-bottom: none;
}
.hours-table tbody tr:hover {
  background: rgba(128, 128, 128, 0.08);
}
</style>
