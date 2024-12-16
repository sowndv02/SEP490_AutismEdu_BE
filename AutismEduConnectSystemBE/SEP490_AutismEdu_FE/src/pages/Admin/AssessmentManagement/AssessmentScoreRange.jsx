import DeleteIcon from '@mui/icons-material/Delete';
import { IconButton, Paper, Typography } from '@mui/material';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import ConfirmDialog from '~/components/ConfirmDialog';
import services from '~/plugins/services';
import ScoreRangeCreation from './ScoreRangeCreation';
import ScoreRangeUpdate from './ScoreRangeUpdate';
function AssessmentScoreRange() {
    const [scoreRanges, setScoreRanges] = useState([]);
    const [openConfirm, setOpenConfirm] = useState(false);
    const [currentItem, setCurrentItem] = useState(null);
    useEffect(() => {
        getScoreRange();
    }, [])

    const getScoreRange = async () => {
        try {
            await services.ScoreRangeAPI.getListScoreRange((res) => {
                const sortedArr = res.result.sort((a, b) => {
                    if (a.minScore === b.minScore) {
                        return a.maxScore - b.maxScore
                    } else {
                        return a.minScore - b.maxScore
                    }
                })
                setScoreRanges(sortedArr)
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        }
    }
    const formatDate = (date) => {
        if (!date) return "";
        const d = new Date(date);
        return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    }

    const handleDelete = async () => {
        try {
            await services.ScoreRangeAPI.deleteScoreRange(currentItem.id, (res) => {
                const filterArr = scoreRanges.filter((s) => {
                    return s.id !== currentItem.id
                })
                setScoreRanges(filterArr);
                setOpenConfirm(false);
                enqueueSnackbar("Xoá thành công", { variant: "success" })
            }, (error) => {
                enqueueSnackbar(error.error[0], { variant: "error" })
            })
        } catch (error) {
            enqueueSnackbar("Xoá thất bại", { variant: "error" })
        }
    }
    return (
        <Paper variant="elevation" sx={{
            p: 3
        }}>
            <Typography variant='h4' mb={3}>Đánh Giá Chung</Typography>
            <ScoreRangeCreation scoreRanges={scoreRanges} setScoreRanges={setScoreRanges} />
            <TableContainer component={Paper} sx={{ mt: 5 }}>
                <Table sx={{ minWidth: 650 }} aria-label="simple table">
                    <TableHead>
                        <TableRow>
                            <TableCell>STT</TableCell>
                            <TableCell align='center'>Khoảng điểm</TableCell>
                            <TableCell sx={{ maxWidth: "300px" }}>Đánh giá</TableCell>
                            <TableCell>Ngày tạo</TableCell>
                            <TableCell>Hành động</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {
                            scoreRanges.length !== 0 && scoreRanges.map((s, index) => {
                                return (
                                    <TableRow key={s.id}>
                                        <TableCell>
                                            {index + 1}
                                        </TableCell>
                                        <TableCell align='center'>
                                            {s.minScore} - {s.maxScore}
                                        </TableCell>
                                        <TableCell sx={{ maxWidth: "300px" }}>
                                            {s.description}
                                        </TableCell>
                                        <TableCell>
                                            {formatDate(s.createDate)}
                                        </TableCell>
                                        <TableCell>
                                            <IconButton
                                                sx={{ color: '#ff3e1d' }}
                                                onClick={() => { setOpenConfirm(true); setCurrentItem(s) }}
                                            ><DeleteIcon /></IconButton>
                                            <ScoreRangeUpdate currentScoreRange={s} scoreRanges={scoreRanges}
                                                currentIndex={index}
                                                setScoreRanges={setScoreRanges} />
                                        </TableCell>
                                    </TableRow>
                                )
                            })
                        }
                    </TableBody>
                </Table>
            </TableContainer>
            <ConfirmDialog openConfirm={openConfirm} setOpenConfirm={setOpenConfirm}
                title={"Xoá nhận xét"}
                content={"Bạn có muốn xoá nhận xết này!"}
                handleAction={handleDelete}
            />
        </Paper>
    )
}

export default AssessmentScoreRange
