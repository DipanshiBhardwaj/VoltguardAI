// Registers the Chart.js pieces the app uses, once. Import this module from any
// component that renders a vue-chartjs chart.
import {
  Chart as ChartJS,
  BarElement,
  CategoryScale,
  Filler,
  Legend,
  LinearScale,
  LineElement,
  PointElement,
  Tooltip,
} from 'chart.js'

ChartJS.register(BarElement, CategoryScale, Filler, Legend, LinearScale, LineElement, PointElement, Tooltip)

export const CHART_COLORS = {
  primary: '#2563eb',
  secondary: '#06b6d4',
  band: 'rgba(16, 185, 129, 0.12)',
  bandLine: 'rgba(16, 185, 129, 0.8)',
  projected: '#f59e0b',
}
