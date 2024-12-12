import { createSlice } from '@reduxjs/toolkit'
export const tutorSlice = createSlice({
    name: 'tutor',
    initialState: {
        value: null
    },
    reducers: {
        setTutorInformation: (state, action) => {
            state.value = action.payload;
        }
    }
})

export const { setTutorInformation } = tutorSlice.actions

export const tutorInfor = (state) => state.tutor.value
export default tutorSlice.reducer