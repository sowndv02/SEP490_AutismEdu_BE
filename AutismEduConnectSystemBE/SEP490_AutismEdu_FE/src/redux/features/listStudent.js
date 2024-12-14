import { createSlice } from '@reduxjs/toolkit'
export const listStudentSlice = createSlice({
    name: 'listStudent',
    initialState: {
        value: []
    },
    reducers: {
        setListStudent: (state, action) => {
            state.value = action.payload;
        }
    }
})

export const { setListStudent } = listStudentSlice.actions

export const listStudent = (state) => state.listStudent.value
export default listStudentSlice.reducer