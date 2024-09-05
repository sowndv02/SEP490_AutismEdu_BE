import RemoveIcon from '@mui/icons-material/Remove';
import { IconButton } from '@mui/material';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogTitle from '@mui/material/DialogTitle';
import * as React from 'react';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
function AddClaimDialog({ selected, setApiCall }) {
    const [open, setOpen] = React.useState(false);

    const handleClickOpen = () => {
        setOpen(true);
    };

    const handleClose = () => {
        setOpen(false);
    };

    const handleSubmit = () => {
        setApiCall(2);
        handleClose();
    }
    return (
        <React.Fragment>
            <IconButton onClick={handleClickOpen}>
                <AddCircleOutlineIcon />
            </IconButton>
            <Dialog
                open={open}
                onClose={handleClose}
                aria-labelledby="alert-dialog-title"
                aria-describedby="alert-dialog-description"
            >
                <DialogTitle id="alert-dialog-title">
                    {`Do you want add ${selected} claims`}
                </DialogTitle>
                <DialogActions>
                    <Button onClick={handleSubmit}>Add</Button>
                    <Button onClick={handleClose} autoFocus>
                        Cancle
                    </Button>
                </DialogActions>
            </Dialog>
        </React.Fragment>
    )
}

export default AddClaimDialog
