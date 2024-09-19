import { configureStore } from '@reduxjs/toolkit'
<<<<<<< HEAD

export default configureStore({
  reducer: {}
=======
import userSlice from '../features/userSlice'

export default configureStore({
  reducer: {
    user: userSlice
  }
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
})