import { createSlice } from '@reduxjs/toolkit'
export const adminSlice = createSlice({
    name: 'admin',
    initialState: {
        value: null
    },
    reducers: {
        setAdminInformation: (state, action) => {
            state.value = action.payload;
        }
    }
})

export const { setAdminInformation } = adminSlice.actions

export const adminInfor = (state) => state.admin.value
export default adminSlice.reducer