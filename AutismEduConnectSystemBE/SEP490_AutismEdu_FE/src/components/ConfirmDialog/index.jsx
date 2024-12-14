import { Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, Divider } from '@mui/material'
import { useState } from 'react'
import LoadingComponent from '../LoadingComponent';

function ConfirmDialog({ openConfirm, setOpenConfirm, handleAction, title, content }) {
    const [loading, setLoading] = useState(false);
    const handleSubmit = async () => {
        setLoading(true);
        await handleAction();
        setLoading(false);
    };
    return (
        <Dialog
            open={openConfirm}
            onClose={() => setOpenConfirm(false)}
            aria-labelledby="alert-dialog-title"
            aria-describedby="alert-dialog-description"
        >
            <DialogTitle id="alert-dialog-title" variant='h5' textAlign={'center'}>
                {title}
            </DialogTitle>
            <Divider/>
            <DialogContent>
                <DialogContentText id="alert-dialog-description">
                    {content}
                </DialogContentText>
            </DialogContent>
            <DialogActions>
                <Button variant='outlined' color='inherit' onClick={() => setOpenConfirm(false)} autoFocus>
                    Huỷ bỏ
                </Button>
                <Button onClick={handleSubmit} variant='contained' color='primary'>Đồng ý</Button>
            </DialogActions>
            <LoadingComponent open={loading} />
        </Dialog>
    )
}

export default ConfirmDialog
