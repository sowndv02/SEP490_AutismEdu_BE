import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Divider, Grid, TextField } from '@mui/material'
import React from 'react'
import ReactQuill from 'react-quill'

function DescriptionModal({ open, handleClose, curriculum }) {
    return (
        <Dialog fullWidth open={open} onClose={handleClose}>
            <DialogTitle variant='h5' textAlign={'center'}>Miêu tả</DialogTitle>
            <Divider />
            <DialogContent>
                <Grid container spacing={2}>

                    <Grid item xs={6} md={6}>
                        <TextField
                            aria-readonly
                            type='number'
                            fullWidth
                            label="Tuổi từ"
                            variant="outlined"
                            name="startAge"
                            value={curriculum?.ageFrom || ''}
                        />
                    </Grid>
                    <Grid item xs={6} md={6}>
                        <TextField
                            aria-readonly
                            type='number'
                            fullWidth
                            label="Đến"
                            variant="outlined"
                            name="endAge"
                            value={curriculum?.ageEnd || ''}
                        />
                    </Grid>
                    <Grid item xs={12} md={12}>
                        <ReactQuill
                            readOnly
                            value={curriculum.description || ''}
                        />
                    </Grid>
                </Grid>
            </DialogContent>
            <DialogActions>
                <Button variant='contained' color='primary' onClick={handleClose}>Đóng</Button>
            </DialogActions>
        </Dialog>
    )
}

export default DescriptionModal
