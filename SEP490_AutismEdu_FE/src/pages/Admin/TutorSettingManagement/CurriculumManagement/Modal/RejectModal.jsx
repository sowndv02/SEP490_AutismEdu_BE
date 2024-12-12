import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Divider, TextField } from '@mui/material'
import React from 'react'
import ReactQuill from 'react-quill'

function RejectModal({ open, handleClose, reason }) {
    return (
        <Dialog fullWidth open={open} onClose={handleClose}>
            <DialogTitle variant='h5' textAlign={'center'}>Lý do từ chối</DialogTitle>
            <Divider />
            <DialogContent>
                <TextField
                    readOnly
                    fullWidth
                    id="outlined-multiline-static"
                    label="Lý do từ chối"
                    multiline
                    color='error'
                    focused
                    rows={4}
                    value={reason || ''}
                />
            </DialogContent>
            <DialogActions>
                <Button variant='contained' color='primary' onClick={handleClose}>Đóng</Button>
            </DialogActions>
        </Dialog>
    )
}

export default RejectModal
